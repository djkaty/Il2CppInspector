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

        // Generate custom attribute with named property arguments that calls default constructor
        // 'module' is the module that owns 'type'; type.Module may still be null when this is called
        public static CustomAttribute AddAttribute(this TypeDefUser type, ModuleDefUser module, TypeDefUser attrTypeDef, params (string prop, object value)[] args) {

            // Resolution scope is the module that needs the reference
            var attRef = new TypeRefUser(attrTypeDef.Module, attrTypeDef.Namespace, attrTypeDef.Name, module);
            var attCtorRef = new MemberRefUser(attrTypeDef.Module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attRef);

            // Attribute arguments
            var attrArgs = args.Select(a =>
                new CANamedArgument(false, module.CorLibTypes.String, a.prop, new CAArgument(module.CorLibTypes.String, a.value)));

            var attr = new CustomAttribute(attCtorRef, null, attrArgs);

            type.CustomAttributes.Add(attr);
            return attr;
        }
    }

    // Output module to create .NET DLLs containing type definitions
    public class AssemblyShims
    {
        // .NET type model
        private readonly TypeModel model;

        // Target folder for DLLs
        private string outputPath;

        // Our custom attributes
        private TypeDefUser addressAttribute;
        private TypeDefUser fieldOffsetAttribute;
        private TypeDefUser attributeAttribute;
        private TypeDefUser metadataOffsetAttribute;
        private TypeDefUser tokenAttribute;

        // Resolver
        private ModuleContext context;
        private AssemblyResolver resolver;

        // The namespace for our custom types
        private const string rootNamespace = "Il2CppInspector.DLL"; // Il2CppDummyDll

        public AssemblyShims(TypeModel model) => this.model = model;

        // Generate base DLL with our custom types
        private ModuleDef CreateBaseAssembly() {
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
            addressAttribute = createAttribute("AddressAttribute");
            addressAttribute.Fields.Add(new FieldDefUser("RVA", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("VA", stringField, FieldAttributes.Public));
            addressAttribute.Fields.Add(new FieldDefUser("Slot", stringField, FieldAttributes.Public));
            addressAttribute.AddDefaultConstructor(attributeCtorRef);

            fieldOffsetAttribute = createAttribute("FieldOffsetAttribute");
            fieldOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            fieldOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            attributeAttribute = createAttribute("AttributeAttribute");
            attributeAttribute.Fields.Add(new FieldDefUser("Name", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("RVA", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            attributeAttribute.AddDefaultConstructor(attributeCtorRef);

            metadataOffsetAttribute = createAttribute("MetadataOffsetAttribute");
            metadataOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            metadataOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            tokenAttribute = createAttribute("TokenAttribute");
            tokenAttribute.Fields.Add(new FieldDefUser("Token", stringField, FieldAttributes.Public));
            tokenAttribute.AddDefaultConstructor(attributeCtorRef);

            return module;
        }

        // Create a new DLL assembly definition
        private ModuleDefUser CreateAssembly(string name) {
            // Create module
            var module = new ModuleDefUser(name) { Kind = ModuleKind.Dll };

            // Set resolution scope
            //module.Context = context;

            // Add module to resolver
            //resolver.AddToCache(module);

            // Create assembly
            var ourVersion = Assembly.GetAssembly(typeof(Il2CppInspector)).GetName().Version;
            var asm = new AssemblyDefUser(name.Replace(".dll", ""), ourVersion);
            asm.Modules.Add(module);
            return module;
        }

        // Generate type recursively with all nested types
        private TypeDefUser AddType(ModuleDefUser module, TypeInfo type) {

            // Generate type with all nested types
             TypeDefUser CreateType(ModuleDefUser module, TypeInfo type) {
                var mType = new TypeDefUser(type.Namespace, type.BaseName) { Attributes = (TypeAttributes) type.Attributes };

                foreach (var nestedType in type.DeclaredNestedTypes)
                    mType.NestedTypes.Add(CreateType(module, nestedType));

                // Add token attribute
                mType.AddAttribute(module, tokenAttribute, ("Token", $"0x{type.Definition.token}"));

                return mType;
            }

            // Add type to module
            var mType = CreateType(module, type);
            module.Types.Add(mType);
            return mType;
        }

        // Generate and save all DLLs
        public void Write(string outputPath) {
            
            // Create folder for DLLs
            this.outputPath = outputPath;
            Directory.CreateDirectory(outputPath);

            // Create resolver
            //context = ModuleDef.CreateModuleContext();
            //resolver = context.AssemblyResolver as AssemblyResolver;

            // Generate our custom types assembly
            var baseDll = CreateBaseAssembly();

            // Write base assembly to disk
            baseDll.Write(Path.Combine(outputPath, baseDll.Name));

            // Generate all application assemblies and types
            var assemblies = new List<ModuleDefUser>();

            foreach (var asm in model.Assemblies) {
                var module = CreateAssembly(asm.ShortName);

                foreach (var type in asm.DefinedTypes.Where(t => !t.IsNested))
                    AddType(module, type);

                assemblies.Add(module);
            }

            // Write all assemblies to disk
            foreach (var asm in assemblies)
                asm.Write(Path.Combine(outputPath, asm.Name));
        }
    }
}
