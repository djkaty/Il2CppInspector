/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp
{
    // Class that represents a composite IL/C++ type
    public class CppModelType
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
        public uint VirtualAddress { get; internal set; }
    }

    // Class that represents the entire structure of the IL2CPP binary realized as C++ types and code,
    // correlated with .NET types where applicable. Primarily designed to enable automated static analysis of disassembly code.
    public class CppApplicationModel
    {
        public CppCompiler.Type Compiler { get; }
        public UnityVersion UnityVersion { get; }
        public CppTypes Types { get; }
        public Il2CppModel ILModel { get; }
        public List<Export> Exports { get; }
        public int WordSize => ILModel.Package.BinaryImage.Bits;

        public CppApplicationModel(Il2CppModel model, UnityVersion unityVersion, CppCompiler.Type compiler = CppCompiler.Type.BinaryFormat) {
            // Set key properties
            Compiler = compiler == CppCompiler.Type.BinaryFormat ? CppCompiler.GuessFromImage(model.Package.BinaryImage) : compiler;
            UnityVersion = unityVersion;
            ILModel = model;

            // Start creation of type model by parsing all of the Unity IL2CPP headers
            Types = CppTypes.FromUnityVersion(unityVersion, WordSize);

            // TODO: Process every type in the binary
            var decl = new CppDeclarationGenerator(ILModel, UnityVersion);

            // Add addresses of IL2CPP API function exports
            Exports = model.Package.Binary.Image.GetExports().ToList();
        }
    }
}
