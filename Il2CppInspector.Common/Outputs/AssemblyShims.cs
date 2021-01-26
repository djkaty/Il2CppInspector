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
using BindingFlags = System.Reflection.BindingFlags;

namespace Il2CppInspector.Outputs
{
    public static class dnlibExtensions
    {
        // Add a default parameterless constructor that calls a specified base constructor
        public static MethodDef AddDefaultConstructor(this TypeDef type, IMethod @base) {
            var ctor = new MethodDefUser(".ctor", MethodSig.CreateInstance(type.Module.CorLibTypes.Void),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var ctorBody = new CilBody();
            ctorBody.Instructions.Add(OpCodes.Ldarg_0.ToInstruction());
            ctorBody.Instructions.Add(OpCodes.Call.ToInstruction(@base));
            ctorBody.Instructions.Add(OpCodes.Ret.ToInstruction());
            ctor.Body = ctorBody;

            type.Methods.Add(ctor);
            return ctor;
        }

        // Add custom attribute to item with named property arguments
        // 'module' is the module that owns 'type'; type.Module may still be null when this is called
        public static CustomAttribute AddAttribute(this IHasCustomAttribute def, ModuleDef module, TypeDef attrTypeDef, params (string prop, object value)[] args) {

            // If SuppressMetadata is set, our own attributes will never be generated so attrTypeDef will be null
            if (attrTypeDef == null)
                return null;

            var attRef = module.Import(attrTypeDef);
            var attCtorRef = new MemberRefUser(attrTypeDef.Module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attRef);

            // Attribute arguments
            var attrArgs = args.Select(a =>
                new CANamedArgument(true, module.CorLibTypes.String, a.prop, new CAArgument(module.CorLibTypes.String, a.value)));

            var attr = new CustomAttribute(attCtorRef, null, attrArgs);

            def.CustomAttributes.Add(attr);
            return attr;
        }
    }

    // Output module to create .NET DLLs containing type definitions
    public class AssemblyShims
    {
        // Suppress informational attributes
        public bool SuppressMetadata { get; set; }

        // .NET type model
        private readonly TypeModel model;

        // Our custom attributes
        private TypeDef addressAttribute;
        private TypeDef fieldOffsetAttribute;
        private TypeDef staticFieldOffsetAttribute;
        private TypeDef attributeAttribute;
        private TypeDef metadataOffsetAttribute;
        private TypeDef metadataPreviewAttribute;
        private TypeDef tokenAttribute;

        // The namespace for our custom types
        private const string rootNamespace = "Il2CppInspector.DLL";

        // All modules (single-module assemblies)
        private Dictionary<Assembly, ModuleDef> modules = new Dictionary<Assembly, ModuleDef>();

        // Custom attributes we will apply directly instead of with a custom attribute function pointer
        private Dictionary<TypeInfo, TypeDef> directApplyAttributes;

        public AssemblyShims(TypeModel model) => this.model = model;

        // Generate base DLL with our custom types
        private ModuleDef CreateBaseAssembly() {
            // Create DLL with our custom types
            var module = CreateAssembly("Il2CppInspector.dll");

            // Import our IL2CPP application's copy of System.Attribute
            // to avoid introducing a dependency on System.Private.CoreLib (.NET Core) from Il2CppInspector itself
            var attributeType = model.TypesByFullName["System.Attribute"];
            var attributeCtor = attributeType.DeclaredConstructors.First(c => !c.IsPublic && !c.IsStatic);
            var attributeTypeRef = GetTypeRef(module, attributeType);
            var attributeCtorRef = new MemberRefUser(attributeTypeRef.Module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attributeTypeRef);

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

            staticFieldOffsetAttribute = createAttribute("StaticFieldOffsetAttribute");
            staticFieldOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            staticFieldOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            attributeAttribute = createAttribute("AttributeAttribute");
            attributeAttribute.Fields.Add(new FieldDefUser("Name", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("RVA", stringField, FieldAttributes.Public));
            attributeAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            attributeAttribute.AddDefaultConstructor(attributeCtorRef);

            metadataOffsetAttribute = createAttribute("MetadataOffsetAttribute");
            metadataOffsetAttribute.Fields.Add(new FieldDefUser("Offset", stringField, FieldAttributes.Public));
            metadataOffsetAttribute.AddDefaultConstructor(attributeCtorRef);

            metadataPreviewAttribute = createAttribute("MetadataPreviewAttribute");
            metadataPreviewAttribute.Fields.Add(new FieldDefUser("Data", stringField, FieldAttributes.Public));
            metadataPreviewAttribute.AddDefaultConstructor(attributeCtorRef);

            tokenAttribute = createAttribute("TokenAttribute");
            tokenAttribute.Fields.Add(new FieldDefUser("Token", stringField, FieldAttributes.Public));
            tokenAttribute.AddDefaultConstructor(attributeCtorRef);

            return module;
        }

        // Create a new DLL assembly definition
        private ModuleDefUser CreateAssembly(string name) {
            // Create module
            var module = new ModuleDefUser(name) { Kind = ModuleKind.Dll };

            // Create assembly
            var ourVersion = System.Reflection.Assembly.GetAssembly(typeof(Il2CppInspector)).GetName().Version;
            var asm = new AssemblyDefUser(name.Replace(".dll", ""), ourVersion);

            // Add module to assembly
            asm.Modules.Add(module);
            return module;
        }
 
        // Generate type recursively with all nested types
        private TypeDefUser CreateType(ModuleDef module, TypeInfo type) {
            // Initialize with base class
            var mType = new TypeDefUser(type.Namespace, type.BaseName, GetTypeRef(module, type.BaseType)) {
                Attributes = (TypeAttributes) type.Attributes
            };

            // Generic parameters
            foreach (var gp in type.GenericTypeParameters) {
                var p = new GenericParamUser((ushort) gp.GenericParameterPosition, (GenericParamAttributes) gp.GenericParameterAttributes, gp.Name);

                // Generic constraints (types and interfaces)
                foreach (var c in gp.GetGenericParameterConstraints())
                    p.GenericParamConstraints.Add(new GenericParamConstraintUser(GetTypeRef(module, c)));

                mType.GenericParameters.Add(p);
            }

            // Interfaces
            foreach (var @interface in type.ImplementedInterfaces)
                mType.Interfaces.Add(new InterfaceImplUser(GetTypeRef(module, @interface)));

            // Add nested types
            foreach (var nestedType in type.DeclaredNestedTypes)
                mType.NestedTypes.Add(CreateType(module, nestedType));

            // Add fields
            foreach (var field in type.DeclaredFields)
                AddField(module, mType, field);

            // Add properties
            foreach (var prop in type.DeclaredProperties)
                AddProperty(module, mType, prop);

            // Add events
            foreach (var evt in type.DeclaredEvents)
                AddEvent(module, mType, evt);

            // Add methods that aren't properties or events
            var props = type.DeclaredProperties.SelectMany(p => new[] { p.GetMethod, p.SetMethod }).Where(m => m != null);
            var events = type.DeclaredEvents.SelectMany(p => new[] { p.AddMethod, p.RemoveMethod, p.RaiseMethod }).Where(m => m != null);

            foreach (var method in type.DeclaredConstructors.AsEnumerable<MethodBase>().Concat(type.DeclaredMethods).Except(props).Except(events))
                AddMethod(module, mType, method);

            // Add token attribute
            if (type.Definition != null)
                mType.AddAttribute(module, tokenAttribute, ("Token", $"0x{type.MetadataToken:X8}"));

            // Add custom attribute attributes
            foreach (var ca in type.CustomAttributes)
                AddCustomAttribute(module, mType, ca);

            return mType;
        }

        // Add a field to a type
        private FieldDef AddField(ModuleDef module, TypeDef mType, FieldInfo field) {
            var s = new FieldSig(GetTypeSig(module, field.FieldType));

            var mField = new FieldDefUser(field.Name, s, (FieldAttributes) field.Attributes);

            // Default value
            if (field.HasDefaultValue)
                mField.Constant = new ConstantUser(field.DefaultValue);

            // Add offset attribute if no default value but metadata present
            else if (field.HasFieldRVA || field.IsLiteral)
                mField.AddAttribute(module, metadataOffsetAttribute, ("Offset", $"0x{field.DefaultValueMetadataAddress:X8}"));

            // Static array initializer preview
            if (field.HasFieldRVA) {
                var preview = model.Package.Metadata.ReadBytes((long) field.DefaultValueMetadataAddress, 8);
                var previewText = string.Join(" ", preview.Select(b => $"{b:x2}"));

                mField.AddAttribute(module, metadataPreviewAttribute, ("Data", previewText));
            }

            // Field offset
            if (!field.IsStatic)
                mField.AddAttribute(module, fieldOffsetAttribute, ("Offset", $"0x{field.Offset:X2}"));
            else if (!field.IsLiteral)
                mField.AddAttribute(module, staticFieldOffsetAttribute, ("Offset", $"0x{field.Offset:X2}"));

            // Add token attribute
            mField.AddAttribute(module, tokenAttribute, ("Token", $"0x{field.MetadataToken:X8}"));

            // Add custom attribute attributes
            foreach (var ca in field.CustomAttributes)
            AddCustomAttribute(module, mField, ca);

            mType.Fields.Add(mField);
            return mField;
        }

        // Add a property to a type
        private PropertyDef AddProperty(ModuleDef module, TypeDef mType, PropertyInfo prop) {
            PropertySig s;

            // Static or instance
            if (prop.GetMethod?.IsStatic ?? prop.SetMethod.IsStatic)
                s = PropertySig.CreateStatic(GetTypeSig(module, prop.PropertyType));
            else
                s = PropertySig.CreateInstance(GetTypeSig(module, prop.PropertyType));

            var mProp = new PropertyDefUser(prop.Name, s, (PropertyAttributes) prop.Attributes);

            mProp.GetMethod = AddMethod(module, mType, prop.GetMethod);
            mProp.SetMethod = AddMethod(module, mType, prop.SetMethod);

            // Add token attribute
            // Generic properties and constructed properties (from disperate get/set methods) have no definition
            if (prop.Definition != null)
                mProp.AddAttribute(module, tokenAttribute, ("Token", $"0x{prop.MetadataToken:X8}"));

            // Add custom attribute attributes
            foreach (var ca in prop.CustomAttributes)
                AddCustomAttribute(module, mProp, ca);

            // Add property to type
            mType.Properties.Add(mProp);
            return mProp;
        }

        // Add an event to a type
        private EventDef AddEvent(ModuleDef module, TypeDef mType, EventInfo evt) {
            var mEvent = new EventDefUser(evt.Name, GetTypeRef(module, evt.EventHandlerType), (EventAttributes) evt.Attributes);

            mEvent.AddMethod = AddMethod(module, mType, evt.AddMethod);
            mEvent.RemoveMethod = AddMethod(module, mType, evt.RemoveMethod);
            mEvent.InvokeMethod = AddMethod(module, mType, evt.RaiseMethod);

            // Add token attribute
            mEvent.AddAttribute(module, tokenAttribute, ("Token", $"0x{evt.MetadataToken:X8}"));

            // Add custom attribute attributes
            foreach (var ca in evt.CustomAttributes)
                AddCustomAttribute(module, mEvent, ca);

            // Add property to type
            mType.Events.Add(mEvent);
            return mEvent;
        }

        // Add a method to a type
        private MethodDef AddMethod(ModuleDef module, TypeDef mType, MethodBase method) {
            // Undefined method
            if (method == null)
                return null;

            // Return type and parameter signature
            MethodSig s;
            
            // Static or instance
            if (method.IsStatic)
                s = MethodSig.CreateStatic(
                    method is MethodInfo mi ? GetTypeSig(module, mi.ReturnType) : module.CorLibTypes.Void,
                    method.DeclaredParameters.Select(p => GetTypeSig(module, p.ParameterType))
                    .ToArray());
            else
                s = MethodSig.CreateInstance(
                    method is MethodInfo mi? GetTypeSig(module, mi.ReturnType) : module.CorLibTypes.Void,
                    method.DeclaredParameters.Select(p => GetTypeSig(module, p.ParameterType))
                    .ToArray());

            // Definition
            var mMethod = new MethodDefUser(method.Name, s, (MethodImplAttributes) method.MethodImplementationFlags, (MethodAttributes) method.Attributes);

            // Generic type parameters
            foreach (var gp in method.GetGenericArguments()) {
                var p = new GenericParamUser((ushort) gp.GenericParameterPosition, (GenericParamAttributes) gp.GenericParameterAttributes, gp.Name);

                // Generic constraints (types and interfaces)
                foreach (var c in gp.GetGenericParameterConstraints())
                    p.GenericParamConstraints.Add(new GenericParamConstraintUser(GetTypeRef(module, c)));

                mMethod.GenericParameters.Add(p);
            }

            // Parameter names and default values
            foreach (var param in method.DeclaredParameters) {
                var p = new ParamDefUser(param.Name, (ushort) (param.Position + 1), (ParamAttributes) param.Attributes);

                if (param.HasDefaultValue)
                    p.Constant = new ConstantUser(param.DefaultValue);

                // Add offset attribute if metadata present
                if (param.DefaultValueMetadataAddress != 0)
                    p.AddAttribute(module, metadataOffsetAttribute, ("Offset", $"0x{param.DefaultValueMetadataAddress:X8}"));

                // Add custom attribute attributes
                foreach (var ca in param.CustomAttributes)
                    AddCustomAttribute(module, p, ca);

                mMethod.ParamDefs.Add(p);
            }

            // Everything that's not extern, abstract or a delegate type should have a method body
            if ((method.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) == 0
                && method.DeclaringType.BaseType?.FullName != "System.MulticastDelegate"
                && !method.IsAbstract) {
                mMethod.Body = new CilBody();
                var inst = mMethod.Body.Instructions;

                // Return nothing if return type is void
                if (mMethod.ReturnType.FullName == "System.Void")
                    inst.Add(OpCodes.Ret.ToInstruction());

                // Return default for value type or enum
                else if (mMethod.ReturnType.IsValueType || ((MethodInfo) method).ReturnType.IsEnum) {
                    var result = new Local(mMethod.ReturnType);
                    mMethod.Body.Variables.Add(result);

                    inst.Add(OpCodes.Ldloca_S.ToInstruction(result));
                    // NOTE: This line creates a reference to an external mscorlib.dll, which we'd prefer to avoid
                    inst.Add(OpCodes.Initobj.ToInstruction(mMethod.ReturnType.ToTypeDefOrRef()));
                    inst.Add(OpCodes.Ldloc_0.ToInstruction());
                    inst.Add(OpCodes.Ret.ToInstruction());
                }

                // Return null for reference types
                else {
                    inst.Add(OpCodes.Ldnull.ToInstruction());
                    inst.Add(OpCodes.Ret.ToInstruction());
                }
            }

            // Add token attribute
            mMethod.AddAttribute(module, tokenAttribute, ("Token", $"0x{method.MetadataToken:X8}"));

            // Add method pointer attribute
            if (method.VirtualAddress.HasValue) {
                var args = new List<(string,object)> {
                        ("RVA", (method.VirtualAddress.Value.Start - model.Package.BinaryImage.ImageBase).ToAddressString()),
                        ("Offset", string.Format("0x{0:X}", model.Package.BinaryImage.MapVATR(method.VirtualAddress.Value.Start))),
                        ("VA", method.VirtualAddress.Value.Start.ToAddressString())
                    };
                if (method.Definition.slot != ushort.MaxValue)
                    args.Add(("Slot", method.Definition.slot.ToString()));

                mMethod.AddAttribute(module, addressAttribute, args.ToArray());
            }

            // Add custom attribute attributes
            foreach (var ca in method.CustomAttributes)
                AddCustomAttribute(module, mMethod, ca);

            // Add method to type
            mType.Methods.Add(mMethod);
            return mMethod;
        }

        // Add a custom attributes attribute to an item, or the attribute itself if it is in our direct apply list
        private CustomAttribute AddCustomAttribute(ModuleDef module, IHasCustomAttribute def, CustomAttributeData ca) {
            if (directApplyAttributes.TryGetValue(ca.AttributeType, out var attrDef) && attrDef != null)
                return def.AddAttribute(module, attrDef);

            return def.AddAttribute(module, attributeAttribute,
                ("Name", ca.AttributeType.Name),
                ("RVA", (ca.VirtualAddress.Start - model.Package.BinaryImage.ImageBase).ToAddressString()),
                ("Offset", string.Format("0x{0:X}", model.Package.BinaryImage.MapVATR(ca.VirtualAddress.Start)))
            );
        }

        // Generate type recursively with all nested types and add to module
        private TypeDefUser AddType(ModuleDef module, TypeInfo type) {
            var mType = CreateType(module, type);

            // Add to attribute apply list if we're looking for it
            if (directApplyAttributes.ContainsKey(type))
                directApplyAttributes[type] = mType;

            // Add type to module
            module.Types.Add(mType);
            return mType;
        }

        // Convert Il2CppInspector TypeInfo into type reference and import to specified module
        private ITypeDefOrRef GetTypeRef(ModuleDef module, TypeInfo type)
            => GetTypeSig(module, type).ToTypeDefOrRef();

        // Convert Il2CppInspector TypeInfo into type signature and import to specified module
        private TypeSig GetTypeSig(ModuleDef module, TypeInfo type)
            => module.Import(GetTypeSigImpl(module, type));

        // Convert Il2CppInspector TypeInfo into type signature
        private TypeSig GetTypeSigImpl(ModuleDef module, TypeInfo type) {
            if (type == null)
                return null;

            // Generic type parameter (VAR)
            if (type.IsGenericTypeParameter)
                return new GenericVar(type.GenericParameterPosition);

            // Generic method parameter (MVAR)
            if (type.IsGenericMethodParameter)
                return new GenericMVar(type.GenericParameterPosition);

            // Array and single-dimension zero-indexed array (ARRAY / SZARRAY)
            if (type.IsArray)
                if (type.GetArrayRank() == 1)
                    return new SZArraySig(GetTypeSig(module, type.ElementType));
                else
                    return new ArraySig(GetTypeSig(module, type.ElementType), type.GetArrayRank());

            // Pointer (PTR)
            if (type.IsPointer)
                return new PtrSig(GetTypeSig(module, type.ElementType));

            // Reference (BYREF)
            if (type.IsByRef)
                return new ByRefSig(GetTypeSig(module, type.ElementType));

            // Get module that owns the type
            var typeOwnerModule = modules[type.Assembly];
            var typeOwnerModuleRef = new ModuleRefUser(typeOwnerModule);

            // Get reference to type; use nested type as resolution scope if applicable
            var typeSig = new TypeRefUser(typeOwnerModule, type.Namespace, type.BaseName,
                type.DeclaringType != null? (IResolutionScope) GetTypeRef(module, type.DeclaringType).ScopeType : typeOwnerModuleRef)
                .ToTypeSig();

            // Non-generic type (CLASS / VALUETYPE)
            if (!type.GetGenericArguments().Any())
                return typeSig;

            // Generic type requires generic arguments (GENERICINST)
            var genericInstSig = new GenericInstSig(typeSig.ToClassOrValueTypeSig(), type.GenericTypeArguments.Length);

            foreach (var gp in type.GetGenericArguments())
                genericInstSig.GenericArguments.Add(GetTypeSig(module, gp));

            return genericInstSig;
        }

        // Generate and save all DLLs
        public void Write(string outputPath, EventHandler<string> statusCallback = null) {

            // Create folder for DLLs
            Directory.CreateDirectory(outputPath);

            // Get all custom attributes with no parameters
            // We'll add these directly to objects instead of the attribute generator function pointer
            directApplyAttributes = model.TypesByDefinitionIndex
                .Where(t => t.BaseType?.FullName == "System.Attribute"
                        && !t.DeclaredFields.Any() && !t.DeclaredProperties.Any())
                .ToDictionary(t => t, t => (TypeDef) null);

            // Generate blank assemblies
            // We have to do this before adding anything else so we can reference every module
            modules.Clear();

            foreach (var asm in model.Assemblies) {
                // Create assembly and add primary module to list
                var module = CreateAssembly(asm.ShortName);
                modules.Add(asm, module);
            }

            // Generate our custom types assembly (relies on mscorlib.dll being added above)
            if (!SuppressMetadata) {
                var baseDll = CreateBaseAssembly();

                // Write base assembly to disk
                baseDll.Write(Path.Combine(outputPath, baseDll.Name));
            }

            // Add assembly custom attribute attributes (must do this after all assemblies are created due to type referencing)
            foreach (var asm in model.Assemblies) {
                var module = modules[asm];

                foreach (var ca in asm.CustomAttributes)
                    AddCustomAttribute(module, module.Assembly, ca);

                // Add token attributes
                module.AddAttribute(module, tokenAttribute, ("Token", $"0x{asm.ImageDefinition.token:X8}"));
                module.Assembly.AddAttribute(module, tokenAttribute, ("Token", $"0x{asm.MetadataToken:X8}"));
            }

            // Add all types
            foreach (var asm in model.Assemblies) {
                statusCallback?.Invoke(this, "Preparing " + asm.ShortName);
                foreach (var type in asm.DefinedTypes.Where(t => !t.IsNested))
                    AddType(modules[asm], type);
            }

            // Write all assemblies to disk
            foreach (var asm in modules.Values) {
                statusCallback?.Invoke(this, "Generating " + asm.Name);
                asm.Write(Path.Combine(outputPath, asm.Name));
            }
        }
    }
}
