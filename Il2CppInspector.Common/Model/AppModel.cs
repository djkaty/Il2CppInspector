/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aron.Weiler;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // Class that represents the entire structure of the IL2CPP binary realized as C++ types and code,
    // correlated with .NET types where applicable. Primarily designed to enable automated static analysis of disassembly code.
    public class AppModel : IEnumerable<CppType>
    {
        // The C++ compiler to target
        public CppCompilerType TargetCompiler { get; private set; }

        // The Unity version used to build the binary
        public UnityVersion UnityVersion { get; private set; }

        // The Unity IL2CPP C++ headers for the binary
        // Use this for code output
        public UnityHeaders UnityHeaders { get; private set; }

        // All of the C++ types used in the application including Unity internal types
        // NOTE: This is for querying individual types for static analysis
        // To generate code output, use DependencyOrderedCppTypes
        public CppTypeCollection CppTypeCollection { get; private set; }

        // All of the C++ types used in the application (.NET type translations only)
        // The types are ordered to enable the production of code output without forward dependencies
        public List<CppType> DependencyOrderedCppTypes { get; private set; }

        // Composite mapping of all the .NET methods in the IL2CPP binary
        public MultiKeyDictionary<MethodBase, CppFnPtrType, AppMethod> Methods { get; } = new MultiKeyDictionary<MethodBase, CppFnPtrType, AppMethod>();

        // Composite mapping of all the .NET types in the IL2CPP binary
        public MultiKeyDictionary<TypeInfo, CppComplexType, AppType> Types { get; } = new MultiKeyDictionary<TypeInfo, CppComplexType, AppType>();

        // All of the string literals in the IL2CPP binary
        // Note: Does not include string literals from global-metadata.dat
        // Note: The virtual addresses are of String* (VAs of the pointer to String*) objects, not the strings themselves
        // For il2cpp < 19, the key is the string literal ordinal instead of the address
        public Dictionary<ulong, string> Strings { get; } = new Dictionary<ulong, string>();

        public bool StringIndexesAreOrdinals => Package.Version < 19;

        // The .NET type model for the application
        public TypeModel TypeModel { get; }

        // All of the exports (including function exports) for the binary
        public List<Export> Exports { get; }

        // All of the symbols representing function names, signatures or type/field names or address labels for the binary
        public Dictionary<string, Symbol> Symbols { get; }

        // All of the API exports defined in the IL2CPP binary
        // Note: Multiple export names may have the same virtual address
        public MultiKeyDictionary<string, ulong, CppFnPtrType> AvailableAPIs { get; } = new MultiKeyDictionary<string, ulong, CppFnPtrType>();

        // Delegated C++ types iterator
        public IEnumerator<CppType> GetEnumerator() => CppTypeCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) CppTypeCollection).GetEnumerator();

        // The C++ declaration generator for this binary
        private CppDeclarationGenerator declarationGenerator;

        // Convenience properties

        // The word size of the binary in bits
        public int WordSizeBits => Image.Bits;

        // The word size of the binary in bytes
        public int WordSizeBytes => WordSizeBits / 8;

        // The binary image
        public IFileFormatStream Image => Package.BinaryImage;

        // The IL2CPP package for this application
        public Il2CppInspector Package => TypeModel.Package;

        // The compiler used to build the binary
        public CppCompilerType SourceCompiler => declarationGenerator.InheritanceStyle;

        // The group that the next added type(s) will be placed in
        private string group = string.Empty;
        private string Group {
            get => group;
            set {
                group = value;
                CppTypeCollection.SetGroup(group);
            }
        }

        // Initialize
        public AppModel(TypeModel model, bool makeDefaultBuild = true) {
            // Save .NET type model
            TypeModel = model;

            // Get addresses of all exports
            Exports = Image.GetExports()?.ToList() ?? new List<Export>();

            // Get all symbols
            Symbols = Image.GetSymbolTable();

            // Build if requested
            if (makeDefaultBuild)
                Build();
        }

        // Build the application model targeting a specific version of Unity and C++ compiler
        // If no Unity version is specified, it will be guessed from the contents of the IL2CPP binary
        // The C++ compiler used to actually build the original IL2CPP binary will always be guessed based on the binary file format
        // (via the constructor of CppDeclarationGenerator, in InheritanceStyle)
        // If no target C++ compiler is specified, it will be set to match the one assumed to have been used to compile the binary
        public AppModel Build(UnityVersion unityVersion = null, CppCompilerType compiler = CppCompilerType.BinaryFormat, bool silent = false) {
            // Don't re-build if not necessary
            var targetCompiler = compiler == CppCompilerType.BinaryFormat ? CppCompiler.GuessFromImage(Image) : compiler;
            if (UnityVersion == unityVersion && TargetCompiler == targetCompiler)
                return this;

            // Silent operation if requested
            var stdout = Console.Out;
            if (silent)
                Console.SetOut(new StreamWriter(Stream.Null));

            // Reset in case this is not the first build
            Methods.Clear();
            Types.Clear();
            Strings.Clear();

            // Set target compiler
            TargetCompiler = targetCompiler;

            // Determine Unity version and get headers
            UnityHeaders = unityVersion != null ? UnityHeaders.GetHeadersForVersion(unityVersion) : UnityHeaders.GuessHeadersForBinary(TypeModel.Package.Binary).Last();
            UnityVersion = unityVersion ?? UnityHeaders.VersionRange.Min;

            Console.WriteLine($"Selected Unity version(s) {UnityHeaders.VersionRange} (types: {UnityHeaders.TypeHeaderResource.VersionRange}, APIs: {UnityHeaders.APIHeaderResource.VersionRange})");

            // Check for matching metadata and binary versions
            if (UnityHeaders.MetadataVersion != Image.Version) {
                Console.WriteLine($"Warning: selected version {UnityVersion} (metadata version {UnityHeaders.MetadataVersion})" +
                                    $" does not match metadata version {Image.Version}.");
            }

            // Initialize declaration generator to process every type in the binary
            declarationGenerator = new CppDeclarationGenerator(this);

            // Start creation of type model by parsing all of the Unity IL2CPP headers
            // Calling declarationGenerator.GenerateRemainingTypeDeclarations() below will automatically add to this collection
            CppTypeCollection = CppTypeCollection.FromUnityHeaders(UnityHeaders, declarationGenerator);

            // Populate AvailableAPIs with actual API symbols from Binary.GetAPIExports() and their matching header signatures
            // NOTE: This will only be filled with exports that actually exist in both the binary and the API header,
            // and have a mappable address. This prevents exceptions when cross-querying the header and binary APIs.
            var exports = TypeModel.Package.Binary.APIExports
                .Where(e => CppTypeCollection.TypedefAliases.ContainsKey(e.Key))
                .Select(e => new {
                    VirtualAddress = e.Value,
                    FnPtr = CppTypeCollection.TypedefAliases[e.Key]
                });

            AvailableAPIs.Clear();
            foreach (var export in exports)
                AvailableAPIs.Add(export.FnPtr.Name, export.VirtualAddress, (CppFnPtrType) export.FnPtr);

            // Initialize ordered type list for code output
            DependencyOrderedCppTypes = new List<CppType>();

            // Add method definitions and types used by them to C++ type model
            Group = "types_from_methods";

            foreach (var method in TypeModel.MethodsByDefinitionIndex.Where(m => m.VirtualAddress.HasValue)) {
                declarationGenerator.IncludeMethod(method);
                AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                var fnPtr = declarationGenerator.GenerateMethodDeclaration(method);
                Methods.Add(method, fnPtr, new AppMethod(method, fnPtr) {Group = Group});
            }

            // Add generic methods definitions and types used by them to C++ type model
            Group = "types_from_generic_methods";

            foreach (var method in TypeModel.GenericMethods.Values.Where(m => m.VirtualAddress.HasValue)) {
                declarationGenerator.IncludeMethod(method);
                AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                var fnPtr = declarationGenerator.GenerateMethodDeclaration(method);
                Methods.Add(method, fnPtr, new AppMethod(method, fnPtr) {Group = Group});
            }

            // Add types from metadata usage list to C++ type model
            // Not supported in il2cpp <19
            Group = "types_from_usages";

            if (Package.MetadataUsages != null)
                foreach (var usage in Package.MetadataUsages) {
                    var address = usage.VirtualAddress;

                    switch (usage.Type) {
                        case MetadataUsageType.StringLiteral:
                            var str = TypeModel.GetMetadataUsageName(usage);
                            Strings.Add(address, str);
                            break;

                        case MetadataUsageType.Type:
                        case MetadataUsageType.TypeInfo:
                            var type = TypeModel.GetMetadataUsageType(usage);
                            declarationGenerator.IncludeType(type);
                            AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                            if (usage.Type == MetadataUsageType.TypeInfo)
                                // Regular type definition
                                Types[type].TypeClassAddress = address;

                            else if (!Types.ContainsKey(type))
                                // Generic type definition has no associated C++ type, therefore no dictionary sub-key
                                Types.Add(type, new AppType(type, null, cppTypeRefPtr: address) {Group = Group});
                            else
                                // Regular type reference
                                Types[type].TypeRefPtrAddress = address;
                            break;
                        case MetadataUsageType.MethodDef:
                        case MetadataUsageType.MethodRef:
                            var method = TypeModel.GetMetadataUsageMethod(usage);
                            declarationGenerator.IncludeMethod(method);
                            AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());
                            
                            // Any method here SHOULD already be in the Methods list
                            // but we have seen one example where this is not the case for a MethodDef
                            if (!Methods.ContainsKey(method)) {
                                var fnPtr = declarationGenerator.GenerateMethodDeclaration(method);
                                Methods.Add(method, fnPtr, new AppMethod(method, fnPtr) {Group = Group});
                            }
                            Methods[method].MethodInfoPtrAddress = address;
                            break;
                    }
                }

            // Add string literals for metadata <19 to the model
            if (Package.Version < 19) {
                /* Version < 19 calls `il2cpp_codegen_string_literal_from_index` to get string literals.
                 * Unfortunately, metadata references are just loose globals in Il2CppMetadataUsage.cpp
                 * so we can't automatically name those. Next best thing is to define an enum for the strings. */
                for (var i = 0; i < Package.StringLiterals.Length; i++) {
                    var str = Package.StringLiterals[i];
                    Strings.Add((ulong) i, str);
                }
            }

            // Find unused concrete value types
            var usedTypes = Types.Values.Select(t => t.Type);
            var unusedTypes = TypeModel.Types.Except(usedTypes);
            var unusedConcreteTypes = unusedTypes.Where(t => !t.IsGenericType && !t.IsGenericParameter
                                                && !t.IsByRef && !t.IsPointer && !t.IsArray && !t.IsAbstract && t.Name != "<Module>");

            Group = "unused_concrete_types";

            foreach (var type in unusedConcreteTypes)
                declarationGenerator.IncludeType(type);
            AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

            // Restore stdout
            Console.SetOut(stdout);

            // Plugin hook to post-process model
            PluginHooks.PostProcessAppModel(this);

            // This is to allow this method to be chained after a new expression
            return this;
        }

        private void AddTypes(List<(TypeInfo ilType, CppComplexType valueType, CppComplexType referenceType, 
            CppComplexType fieldsType, CppComplexType vtableType, CppComplexType staticsType)> types) {

            // Add types to dependency-ordered list
            foreach (var type in types) {
                if (type.vtableType != null)
                    DependencyOrderedCppTypes.Add(type.vtableType);
                if (type.staticsType != null)
                    DependencyOrderedCppTypes.Add(type.staticsType);

                if (type.fieldsType != null)
                    DependencyOrderedCppTypes.Add(type.fieldsType);
                if (type.valueType != null)
                    DependencyOrderedCppTypes.Add(type.valueType);

                DependencyOrderedCppTypes.Add(type.referenceType);
            }

            // Create composite types
            foreach (var type in types)
                if (!Types.ContainsKey(type.ilType))
                    Types.Add(type.ilType, type.referenceType, new AppType(type.ilType, type.referenceType, type.valueType) {Group = Group});
        }

        // Get all the C++ types for a group
        public IEnumerable<CppType> GetCppTypeGroup(string groupName) => CppTypeCollection.GetTypeGroup(groupName);
        public IEnumerable<CppType> GetDependencyOrderedCppTypeGroup(string groupName) => DependencyOrderedCppTypes.Where(t => t.Group == groupName);

        // Get all the composite types for a group
        public IEnumerable<AppType> GetTypeGroup(string groupName) => Types.Values.Where(t => t.Group == groupName);

        // Get all the composite methods for a group
        public IEnumerable<AppMethod> GetMethodGroup(string groupName) => Methods.Values.Where(m => m.Group == groupName);

        // Static analysis tools

        // Get the address map for the model
        // This takes a while to construct so we only build it if requested
        private AddressMap addressMap;
        public AddressMap GetAddressMap() {
            if (addressMap == null)
                addressMap = new AddressMap(this);
            return addressMap;
        }

        // Get the byte offset in Il2CppClass for this app's Unity version to the vtable
        public int GetVTableOffset() => CppTypeCollection.GetComplexType("Il2CppClass")["vtable"].OffsetBytes;

        // Get the vtable method index from an offset from the start of the Il2CppClass
        // Unity 5.3.0-5.3.5 uses MethodInfo** - a pointer to a list of MethodInfo pointers
        // Unity 5.3.6-5.4.6 uses VirtualInvokeData* - a pointer to an array of VirtualInvokeData
        // Unity 5.5.0 onwards moves the VirtualInvokeData to the end of Il2CppClass and makes it an array
        // We only include support for Unity 5.5.0 onwards
        public int GetVTableIndexFromClassOffset(int offset) {
            if (UnityVersion.CompareTo("5.5.0") < 0)
                throw new NotImplementedException("VTable index resolution is only supported for Unity 5.5.0 and later");

            // VirtualInvokeData has two members. The first is the jump target.
            // Il2CppMethodPointer methodPtr;
            // const MethodInfo* method;
            var offsetIntoVTable = offset - GetVTableOffset();
            var vidSize = WordSizeBits == 32? 8 : 16;
            return offsetIntoVTable / vidSize;
        }
    }
}
