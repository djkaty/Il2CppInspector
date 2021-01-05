/*
    Copyright 2017-2020 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Il2CppInspector.Reflection;
using Assembly = System.Reflection.Assembly;
using BindingFlags = System.Reflection.BindingFlags;

namespace Il2CppInspector.Outputs
{
    public static class dnlibExtensions
    {
        // Add a default parameterless constructor that calls a specified base constructor
        public static MethodDefUser AddDefaultConstructor(this TypeDefUser type, IMethod @base) {
            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(type.Module.CorLibTypes.Void),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var ctorBody = new CilBody();
            ctorBody.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctorBody.Instructions.Add(OpCodes.Call.ToInstruction(@base));
            ctorBody.Instructions.Add(OpCodes.Ret.ToInstruction());

            type.Methods.Add(ctor);
            return ctor;
        }
    }

    // Output module to create .NET DLLs containing type definitions
    public class AssemblyShims
    {
        private readonly TypeModel model;

        private string outputPath;

        // The namespace for our custom types
        private const string rootNamespace = "Il2CppInspector.DLL"; // Il2CppDummyDll

        public AssemblyShims(TypeModel model) => this.model = model;

        // Create a new DLL assembly definition
        private ModuleDefUser CreateAssembly(string name) {
            var module = new ModuleDefUser(name) { Kind = ModuleKind.Dll };

            var ourVersion = Assembly.GetAssembly(typeof(Il2CppInspector)).GetName().Version;

            var asm = new AssemblyDefUser(name.Replace(".dll", ""), ourVersion);
            asm.Modules.Add(module);

            return module;
        }

        // Generate base DLL with our custom types
        private void CreateBaseAssembly() {
            // Create DLL with our custom types
            var module = CreateAssembly("Il2CppInspector.dll");

            var importer = new Importer(module);
            var attributeCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
            var attributeTypeRef = importer.Import(typeof(Attribute));
            var attributeCtorRef = importer.Import(attributeCtor);

            var stringField = new FieldSig(module.CorLibTypes.String);

            // Create a type deriving from System.Attribute and add it to the assembly
            TypeDefUser createAttribute(string name) {
                var attribute = new TypeDefUser(rootNamespace, name, attributeTypeRef);
                attribute.Attributes = TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
                module.Types.Add(attribute);
                return attribute;
            }

            // Create our custom attributes for compatibility with Il2CppDumper
            // TODO: New format with numeric values where applicable
            var addressAttribute = createAttribute("AddressAttribute");
            addressAttribute.Fields.Add(new FieldDefUser("RVA", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("VA", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("Slot", stringField, FieldAttributes.Public));
            addressAttribute.AddDefaultConstructor(attributeCtorRef);

            var fieldOffsetAttribute = createAttribute("FieldOffsetAttribute");
            fieldOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            fieldOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            var attributeAttribute = createAttribute("AttributeAttribute");
            attributeAttribute.Fields.Add(new FieldDefUser("Name", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("RVA", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            attributeAttribute.AddDefaultConstructor(attributeCtorRef);

            var metadataOffsetAttribute = createAttribute("MetadataOffsetAttribute");
            metadataOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            metadataOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            var tokenAttribute = createAttribute("TokenAttribute");
            tokenAttribute.Fields.Add(new FieldDefUser("Token", stringField, FieldAttributes.Public));
            tokenAttribute.AddDefaultConstructor(attributeCtorRef);

            // Write DLL to disk
            module.Write(Path.Combine(outputPath, module.Name));
        }

        // Generate and save all DLLs
        public void Write(string outputPath) {
            
            // Create folder for DLLs
            this.outputPath = outputPath;
            Directory.CreateDirectory(outputPath);

            // Generate our custom types assembly
            CreateBaseAssembly();

            // Generate type recursively with all nested types
            TypeDefUser createType(TypeInfo type) {
                var mType = new TypeDefUser(type.Namespace, type.BaseName) { Attributes = (TypeAttributes) type.Attributes };

                foreach (var nestedType in type.DeclaredNestedTypes)
                    mType.NestedTypes.Add(createType(nestedType));

                return mType;
            }

            // Generate all application assemblies and types
            var assemblies = new List<ModuleDefUser>();

            foreach (var asm in model.Assemblies) {
                var module = CreateAssembly(asm.ShortName);

                foreach (var type in asm.DefinedTypes.Where(t => !t.IsNested))
                    module.Types.Add(createType(type));

                assemblies.Add(module);
            }

            // Write all assemblies to disk
            foreach (var asm in assemblies)
                asm.Write(Path.Combine(outputPath, asm.Name));
        }
    }
}
