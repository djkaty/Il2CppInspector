/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Model
{
    // Class that represents a composite IL/C++ type
    public class AppType
    {
        // The corresponding C++ type definition which represents an instance of the object
        // If a .NET type, this is derived from Il2CppObject, otherwise it can be any type
        // If the underlying .NET type is a struct (value type), this will return the boxed version
        public CppType CppType { get; internal set; }

        // For an underlying .NET type which is a struct (value type), the unboxed type, otherwise null
        public CppType CppValueType { get; internal set; }

        // The type in the model this object represents (for .NET types, otherwise null)
        public TypeInfo ILType { get; internal set; }

        // The VA of the Il2CppClass object which defines this type (for .NET types, otherwise zero)
        public ulong VirtualAddress { get; internal set; }
    }

    // Class that represents a composite IL/C++ method
    public class AppMethod
    {
        // The corresponding C++ function pointer type
        public CppFnPtrType CppFnPtrType { get; internal set; }

        // The corresponding .NET method
        public MethodBase ILMethod { get; internal set; }

        // The VA of the MethodInfo* (VA of the pointer to the MethodInfo) object which defines this method
        public ulong MethodInfoPtrAddress { get; internal set; } 

        // The VA of the method code itself, or 0 if unknown/not compiled
        public ulong MethodCodeAddress => ILMethod.VirtualAddress?.Start ?? 0;
    }

    // Class that represents the entire structure of the IL2CPP binary realized as C++ types and code,
    // correlated with .NET types where applicable. Primarily designed to enable automated static analysis of disassembly code.
    public class AppModel : IEnumerable<CppType>
    {
        // The C++ compiler to target
        public CppCompilerType TargetCompiler { get; private set; }

        // The Unity version used to build the binary
        public UnityVersion UnityVersion { get; set; } // TODO: Change to private set after integrating IDA output

        // The Unity IL2CPP C++ headers for the binary
        // Use this for code output
        public UnityHeader UnityHeader { get; set; } // TODO: Change to private set after integrating IDA output

        // All of the C++ types used in the application including Unity internal types
        // NOTE: This is for querying individual types for static analysis
        // To generate code output, use DeclarationOrderedTypes
        public CppTypeCollection TypeCollection { get; set; } // TODO: Change to private set after integrating IDA output

        // All of the C++ types used in the application (.NET type translations only)
        // The types are ordered to enable the production of code output without forward dependencies
        public List<CppType> DependencyOrderedTypes { get; private set; }

        // The .NET type model for the application
        public TypeModel ILModel { get; }

        // All of the function exports for the binary
        public List<Export> Exports { get; }

        // Delegated C++ types iterator
        public IEnumerator<CppType> GetEnumerator() => TypeCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) TypeCollection).GetEnumerator();

        // The C++ declaration generator for this binary
        // TODO: Make this private once IDA output integration is completed
        internal CppDeclarationGenerator declarationGenerator;

        // Convenience properties

        // The word size of the binary in bits
        public int WordSize => ILModel.Package.BinaryImage.Bits;

        // The IL2CPP package for this application
        public Il2CppInspector Package => ILModel.Package;

        // The compiler used to build the binary
        public CppCompilerType SourceCompiler => declarationGenerator.InheritanceStyle;

        // The Unity header text including word size define
        public string UnityHeaderText => (WordSize == 32 ? "#define IS_32BIT\n" : "") + UnityHeader.GetHeaderText();

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
            TypeCollection = CppTypeCollection.FromUnityHeaders(UnityHeader, WordSize);

            // Initialize declaration generator to process every type in the binary
            declarationGenerator = new CppDeclarationGenerator(this);

            // Initialize ordered type list for code output
            DependencyOrderedTypes = new List<CppType>();

            // Add method definitions to C++ type model
            TypeCollection.SetGroup("type_definitions");

            foreach (var method in ILModel.MethodsByDefinitionIndex.Where(m => m.VirtualAddress.HasValue)) {
                declarationGenerator.IncludeMethod(method);
                DependencyOrderedTypes.AddRange(declarationGenerator.GenerateRemainingTypeDeclarations());
            }

            // Add generic methods to C++ type model
            TypeCollection.SetGroup("types_from_generics");

            foreach (var method in ILModel.GenericMethods.Values.Where(m => m.VirtualAddress.HasValue)) {
                declarationGenerator.IncludeMethod(method);
                DependencyOrderedTypes.AddRange(declarationGenerator.GenerateRemainingTypeDeclarations());
            }

            // Add metadata usage types to C++ type model
            // Not supported in il2cpp <19
            TypeCollection.SetGroup("types_from_usages");

            if (Package.MetadataUsages != null)
                foreach (var usage in Package.MetadataUsages) {
                    switch (usage.Type) {
                        case MetadataUsageType.Type:
                        case MetadataUsageType.TypeInfo:
                            var type = ILModel.GetMetadataUsageType(usage);
                            declarationGenerator.IncludeType(type);
                            DependencyOrderedTypes.AddRange(declarationGenerator.GenerateRemainingTypeDeclarations());
                            break;
                        case MetadataUsageType.MethodDef:
                        case MetadataUsageType.MethodRef:
                            var method = ILModel.GetMetadataUsageMethod(usage);
                            declarationGenerator.IncludeMethod(method);
                            DependencyOrderedTypes.AddRange(declarationGenerator.GenerateRemainingTypeDeclarations());
                            break;
                    }
                }

            // TODO: Build composite types

            // This is to allow this method to be chained after a new expression
            return this;
        }

        // Get all the types for a group
        public IEnumerable<CppType> GetTypeGroup(string groupName) => TypeCollection.GetTypeGroup(groupName);
        public IEnumerable<CppType> GetDependencyOrderedTypeGroup(string groupName) => DependencyOrderedTypes.Where(t => t.Group == groupName);
    }
}
