/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aron.Weiler;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // Class that represents a composite IL/C++ type

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
        public UnityHeader UnityHeader { get; private set; }

        // All of the C++ types used in the application including Unity internal types
        // NOTE: This is for querying individual types for static analysis
        // To generate code output, use DependencyOrderedCppTypes
        public CppTypeCollection CppTypeCollection { get; private set; }

        // All of the C++ types used in the application (.NET type translations only)
        // The types are ordered to enable the production of code output without forward dependencies
        public List<CppType> DependencyOrderedCppTypes { get; private set; }

        // Composite mapping of all the .NET methods in the IL2CPP binary
        public MultiKeyDictionary<MethodBase, CppFnPtrType, AppMethod> Methods = new MultiKeyDictionary<MethodBase, CppFnPtrType, AppMethod>();

        // Composite mapping of all the .NET types in the IL2CPP binary
        public MultiKeyDictionary<TypeInfo, CppComplexType, AppType> Types = new MultiKeyDictionary<TypeInfo, CppComplexType, AppType>();

        // All of the string literals in the IL2CPP binary
        // Note: Does not include string literals from global-metadata.dat
        // Note: The virtual addresses are of String* (VAs of the pointer to String*) objects, not the strings themselves
        // For il2cpp < 19, the key is the string literal ordinal instead of the address
        public Dictionary<ulong, string> Strings = new Dictionary<ulong, string>();

        // All of the custom attribute generator functions in the library
        public Dictionary<CustomAttributeData, AppMethod> CustomAttributeGenerators = new Dictionary<CustomAttributeData, AppMethod>();

        public bool StringIndexesAreOrdinals => Package.MetadataUsages == null;

        // The .NET type model for the application
        public TypeModel ILModel { get; }

        // All of the function exports for the binary
        public List<Export> Exports { get; }

        // Delegated C++ types iterator
        public IEnumerator<CppType> GetEnumerator() => CppTypeCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) CppTypeCollection).GetEnumerator();

        // The C++ declaration generator for this binary
        internal CppDeclarationGenerator declarationGenerator; // TODO: Make private when name integration completed

        // Convenience properties

        // The word size of the binary in bits
        public int WordSize => ILModel.Package.BinaryImage.Bits;

        // The IL2CPP package for this application
        public Il2CppInspector Package => ILModel.Package;

        // The compiler used to build the binary
        public CppCompilerType SourceCompiler => declarationGenerator.InheritanceStyle;

        // The Unity header text including word size define
        public string UnityHeaderText => (WordSize == 32 ? "#define IS_32BIT\n" : "") + UnityHeader.GetHeaderText();

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
        public AppModel(TypeModel model) {
            // Save .NET type model
            ILModel = model;

            // Get addresses of IL2CPP API function exports
            Exports = model.Package.Binary.Image.GetExports()?.ToList() ?? new List<Export>();
        }

        // Build the application model targeting a specific version of Unity and C++ compiler
        // If no Unity version is specified, it will be guessed from the contents of the IL2CPP binary
        // The C++ compiler used to actually build the original IL2CPP binary will always be guessed based on the binary file format
        // (via the constructor of CppDeclarationGenerator, in InheritanceStyle)
        // If no target C++ compiler is specified, it will be set to match the one assumed to have been used to compile the binary
        public AppModel Build(UnityVersion unityVersion = null, CppCompilerType compiler = CppCompilerType.BinaryFormat) {
            // Set target compiler
            TargetCompiler = compiler == CppCompilerType.BinaryFormat ? CppCompiler.GuessFromImage(ILModel.Package.BinaryImage) : compiler;

            // Determine Unity version and get headers
            UnityHeader = unityVersion != null ? UnityHeader.GetHeaderForVersion(unityVersion) : UnityHeader.GuessHeadersForModel(ILModel)[0];
            UnityVersion = unityVersion ?? UnityHeader.MinVersion;

            // Check for matching metadata and binary versions
            if (UnityHeader.MetadataVersion != ILModel.Package.BinaryImage.Version) {
                Console.WriteLine($"Warning: selected version {UnityVersion} (metadata version {UnityHeader.MetadataVersion})" +
                                  $" does not match metadata version {ILModel.Package.BinaryImage.Version}.");
            }

            // Start creation of type model by parsing all of the Unity IL2CPP headers
            // Calling declarationGenerator.GenerateRemainingTypeDeclarations() below will automatically add to this collection
            CppTypeCollection = CppTypeCollection.FromUnityHeaders(UnityHeader, WordSize);

            // Initialize declaration generator to process every type in the binary
            declarationGenerator = new CppDeclarationGenerator(this);

            // Initialize ordered type list for code output
            DependencyOrderedCppTypes = new List<CppType>();

            // Add method definitions and types used by them to C++ type model
            Group = "types_from_methods";

            foreach (var method in ILModel.MethodsByDefinitionIndex.Where(m => m.VirtualAddress.HasValue)) {
                declarationGenerator.IncludeMethod(method);
                AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                var fnPtr = declarationGenerator.GenerateMethodDeclaration(method);
                Methods.Add(method, fnPtr, new AppMethod(method, fnPtr) {Group = Group});
            }

            // Add generic methods definitions and types used by them to C++ type model
            Group = "types_from_generic_methods";

            foreach (var method in ILModel.GenericMethods.Values.Where(m => m.VirtualAddress.HasValue)) {
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
                            var str = ILModel.GetMetadataUsageName(usage);
                            Strings.Add(address, str);
                            break;

                        case MetadataUsageType.Type:
                        case MetadataUsageType.TypeInfo:
                            var type = ILModel.GetMetadataUsageType(usage);
                            declarationGenerator.IncludeType(type);
                            AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                            if (usage.Type == MetadataUsageType.TypeInfo)
                                // .NET unsafe pointer type that has not been mapped by IL2CPP to a C type
                                // (often void* or byte* in metadata v23 and v24.0)
                                if (!Types.ContainsKey(type)) {
                                    Debug.Assert(type.IsPointer);

                                    // TODO: This should really be handled by CppDeclarationGenerator, and doesn't generate the full definition
                                    var cppType = CppTypeCollection.Struct(declarationGenerator.TypeNamer.GetName(type));
                                    var cppObjectType = (CppComplexType) CppTypeCollection["Il2CppObject"];
                                    cppType.Fields = new SortedDictionary<int, List<CppField>>(cppObjectType.Fields);

                                    DependencyOrderedCppTypes.Add(cppType);
                                    Types.Add(type, cppType, new AppType(type, cppType, cppClassPtr: address) {Group = Group});
                                }
                                else
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
                            var method = ILModel.GetMetadataUsageMethod(usage);
                            declarationGenerator.IncludeMethod(method);
                            AddTypes(declarationGenerator.GenerateRemainingTypeDeclarations());

                            Methods[method].MethodInfoPtrAddress = address;
                            break;
                    }
                }

            // Add string literals for metadata <19 to the model
            else {
                /* Version < 19 calls `il2cpp_codegen_string_literal_from_index` to get string literals.
                 * Unfortunately, metadata references are just loose globals in Il2CppMetadataUsage.cpp
                 * so we can't automatically name those. Next best thing is to define an enum for the strings. */
                for (var i = 0; i < Package.StringLiterals.Length; i++) {
                    var str = Package.StringLiterals[i];
                    Strings.Add((ulong) i, str);
                }
            }

            // Add custom attribute generators to the model
            foreach (var cppMethod in ILModel.AttributesByIndices.Values.Where(m => m.VirtualAddress.HasValue)) {
                var cppMethodName = declarationGenerator.TypeNamer.GetName(cppMethod.AttributeType) + "_CustomAttributesCacheGenerator";
                var fnPtrType = CppFnPtrType.FromSignature(CppTypeCollection, $"void (*{cppMethodName})(CustomAttributesCache *)");
                fnPtrType.Name = cppMethodName;
                fnPtrType.Group = "custom_attribute_generators";

                var appMethod = new AppMethod(null, fnPtrType, cppMethod.VirtualAddress.Value.Start) {Group = fnPtrType.Group};
                CustomAttributeGenerators.Add(cppMethod, appMethod);
            }

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
    }
}
