/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppInspector.Reflection
{
    // Class representing a MethodBase.Invoke() method for a specific signature
    // Every IL2CPP invoker has the signature:
    // void* RuntimeInvoker_{RequiresObject (True/False)}{ReturnType}_{ParameterTypes}
    // (Il2CppMethodPointer pointer, const RuntimeMethod* methodMetadata, void* obj, void** args)
    public class MethodInvoker
    {
        // IL2CPP invoker index
        public int Index { get; }

        // Virtual address of the invoker function
        public (ulong Start, ulong End) VirtualAddress { get; }

        // If false, the first argument to the called function pointers must be the object instance
        public bool IsStatic { get; }

        // Return type
        public TypeInfo ReturnType { get; }

        // Argument types
        public TypeInfo[] ParameterTypes { get; }

        // Find the correct method invoker for a method with a specific signature
        public MethodInvoker(MethodBase exampleMethod, int index) {
            var model = exampleMethod.Assembly.Model;
            var package = exampleMethod.Assembly.Model.Package;

            Index = index;
            IsStatic = exampleMethod.IsStatic;
            
            ReturnType = exampleMethod.IsConstructor ? model.TypesByFullName["System.Void"] : mapParameterType(model, ((MethodInfo) exampleMethod).ReturnType);
            ParameterTypes = exampleMethod.DeclaredParameters.Select(p => mapParameterType(model, p.ParameterType)).ToArray();

            var start = package.MethodInvokePointers[Index];
            VirtualAddress = (start & 0xffff_ffff_ffff_fffe, package.FunctionAddresses[start]);
        }

        // The invokers use Object for all reference types, and SByte for booleans
        private TypeInfo mapParameterType(TypeModel model, TypeInfo type) => type switch {
            { IsValueType: false }              => model.TypesByFullName["System.Object"],
            { FullName: "System.Boolean" }      => model.TypesByFullName["System.SByte"],
            _                                   => type
        };

        public string Name => $"RuntimeInvoker_{!IsStatic}{ReturnType.BaseName.ToCIdentifier()}_" + string.Join("_", ParameterTypes.Select(p => p.BaseName.ToCIdentifier()));

        // Display as a C++ method signature; MethodInfo* is the same as RuntimeMethod* (see codegen/il2cpp-codegen-metadata.h)
        public string Signature => $"void* {Name}(Il2CppMethodPointer pointer, const MethodInfo* methodMetadata, void* obj, void** args)";

        public override string ToString() => Signature;
    }
}
