/*
    Copyright 2017-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp
{
    // Class for generating C header declarations from Reflection objects (TypeInfo, etc.)
    public class CppDeclarationGenerator
    {
        private readonly AppModel appModel;

        private TypeModel model => appModel.TypeModel;
        private CppTypeCollection types => appModel.CppTypeCollection;

        // Word size (32/64-bit) for this generator
        public int WordSize => appModel.WordSizeBits;

        // Version number and header file to generate structures for
        public UnityVersion UnityVersion => appModel.UnityVersion;

        // How inheritance of type structs should be represented.
        // Different C++ compilers lay out C++ class structures differently,
        // meaning that the compiler must be known in order to generate class type structures
        // with the correct layout.
        public CppCompilerType InheritanceStyle;

        public CppDeclarationGenerator(AppModel appModel) {
            this.appModel = appModel;
            
            InitializeNaming();
            InitializeConcreteImplementations();

            // Configure inheritance style based on binary type; this can be overridden by setting InheritanceStyle in the object initializer
            InheritanceStyle = CppCompiler.GuessFromImage(model.Package.BinaryImage);
        }
        
        // C type declaration used to name variables of the given C# type
        private static Dictionary<string, string> primitiveTypeMap = new Dictionary<string, string> {
            ["Boolean"] = "bool",
            ["Byte"] = "uint8_t",
            ["SByte"] = "int8_t",
            ["Int16"] = "int16_t",
            ["UInt16"] = "uint16_t",
            ["Int32"] = "int32_t",
            ["UInt32"] = "uint32_t",
            ["Int64"] = "int64_t",
            ["UInt64"] = "uint64_t",
            ["IntPtr"] = "void *",
            ["UIntPtr"] = "void *",
            ["Char"] = "uint16_t",
            ["Double"] = "double",
            ["Single"] = "float"
        };

        public CppType AsCType(TypeInfo ti) {
            // IsArray case handled by TypeNamer.GetName
            if (ti.IsByRef || ti.IsPointer) {
                return AsCType(ti.ElementType).AsPointer(WordSize);
            }
            if (ti.IsValueType) {
                if (ti.IsPrimitive && primitiveTypeMap.ContainsKey(ti.Name)) {
                    return types.GetType(primitiveTypeMap[ti.Name]);
                }
                return types.GetType(TypeNamer.GetName(ti));
            }
            if (ti.IsEnum) {
                return types.GetType(TypeNamer.GetName(ti));
            }
            return types.GetType(TypeNamer.GetName(ti) + " *");
        }

        // Resets the cache of visited types and pending types to output, but preserve any names we have already generated
        public void Reset() {
            VisitedFieldStructs.Clear();
            VisitedTypes.Clear();
            TodoFieldStructs.Clear();
            TodoTypeStructs.Clear();
        }

        #region Field Struct Generation
        /* Generating field structures (structures for the fields of a given type) occurs in two passes.
         * In the first pass (VisitFieldStructs), we walk over a type and all of the types that the resulting structure would depend on.
         * In the second pass (GenerateVisitedFieldStructs), we generate all type structures in the necessary order.
         * (For example: structures for value types must precede any usage of those value types for layout reasons).
         */

        // A cache of field structures that have already been generated, to eliminate duplicate definitions
        private readonly HashSet<TypeInfo> VisitedFieldStructs = new HashSet<TypeInfo>();

        // A queue of field structures that need to be generated.
        private readonly List<TypeInfo> TodoFieldStructs = new List<TypeInfo>();

        // Walk over dependencies of the given type, to figure out what field structures it depends on
        private void VisitFieldStructs(TypeInfo ti) {
            if (VisitedFieldStructs.Contains(ti))
                return;
            if (ti.IsByRef || ti.ContainsGenericParameters)
                return;
            VisitedFieldStructs.Add(ti);

            if (ti.BaseType != null)
                VisitFieldStructs(ti.BaseType);

            if (ti.IsArray)
                VisitFieldStructs(ti.ElementType);

            if (ti.IsEnum)
                VisitFieldStructs(ti.GetEnumUnderlyingType());

            foreach (var fi in ti.DeclaredFields)
                if (!fi.IsStatic && !fi.IsLiteral && (fi.FieldType.IsEnum || fi.FieldType.IsValueType))
                    VisitFieldStructs(fi.FieldType);

            TodoFieldStructs.Add(ti);
        }

        // Generate the fields for the base class of all objects (Il2CppObject)
        // The two fields are inlined so that we can specialize the klass member for each type object
        private CppComplexType GenerateObjectStruct(string name, TypeInfo ti) {
            var type = types.Struct(name);
            types.AddField(type, "klass", TypeNamer.GetName(ti) + "__Class *");
            types.AddField(type, "monitor", "MonitorData *");
            return type;
        }

        // Generate structure fields for each field of a given type
        private void GenerateFieldList(CppComplexType type, CppNamespace ns, TypeInfo ti) {
            var namer = ns.MakeNamer<FieldInfo>(field => field.Name.ToCIdentifier());
            foreach (var field in ti.DeclaredFields) {
                if (field.IsLiteral || field.IsStatic)
                    continue;
                type.AddField(namer.GetName(field), AsCType(field.FieldType));
            }
        }

        // Generate the C structure for a value type, such as an enum or struct
        private (CppComplexType valueType, CppComplexType boxedType) GenerateValueFieldStruct(TypeInfo ti) {
            CppComplexType valueType, boxedType;
            string name = TypeNamer.GetName(ti);

            if (ti.IsEnum) {
                // Enums should be represented using enum syntax
                // They otherwise behave like value types
                var namer = CreateNamespace().MakeNamer<FieldInfo>((field) => field.Name.ToCIdentifier());
                var underlyingType = AsCType(ti.GetEnumUnderlyingType());
                valueType = types.Enum(underlyingType, name);
                foreach (var field in ti.DeclaredFields) {
                    if (field.Name != "value__")
                        ((CppEnumType)valueType).AddField(namer.GetName(field), field.DefaultValue);
                }

                boxedType = GenerateObjectStruct(name + "__Boxed", ti);
                boxedType.AddField("value", AsCType(ti));
            } else {
                // This structure is passed by value, so it doesn't include Il2CppObject fields.
                valueType = types.Struct(name);
                GenerateFieldList(valueType, CreateNamespace(), ti);

                // Also generate the boxed form of the structure which includes the Il2CppObject header.
                boxedType = GenerateObjectStruct(name + "__Boxed", ti);
                boxedType.AddField("fields", AsCType(ti));
            }
            return (valueType, boxedType);
        }

        // Generate the C structure for a reference type, such as a class or array
        private (CppComplexType objectOrArrayType, CppComplexType fieldsType) GenerateRefFieldStruct(TypeInfo ti) {
            var name = TypeNamer.GetName(ti);

            if (ti.IsArray) {
                var klassType = ti.IsArray ? ti : ti.BaseType;
                var elementType = ti.IsArray ? AsCType(ti.ElementType) : types.GetType("void *");
                var type = GenerateObjectStruct(name, klassType);
                types.AddField(type, "bounds", "Il2CppArrayBounds *");
                types.AddField(type, "max_length", "il2cpp_array_size_t");
                type.AddField("vector", elementType.AsArray(32));
                return (type, null);
            }

            /* Generate a list of all base classes starting from the root */
            List<TypeInfo> baseClasses = new List<TypeInfo>();
            for (var bti = ti; bti != null; bti = bti.BaseType)
                baseClasses.Add(bti);
            baseClasses.Reverse();

            var ns = CreateNamespace();

            if (InheritanceStyle == CppCompilerType.MSVC) {
                /* MSVC style: classes directly contain their base class as the first member.
                 * This causes all classes to be aligned to the alignment of their base class. */
                TypeInfo firstNonEmpty = null;
                foreach (var bti in baseClasses) {
                    if (bti.DeclaredFields.Any(field => !field.IsStatic && !field.IsLiteral)) {
                        firstNonEmpty = bti;
                        break;
                    }
                }
                if (firstNonEmpty == null) {
                    /* This struct is completely empty. Omit __Fields entirely. */
                    return (GenerateObjectStruct(name, ti), null);
                } else {
                    CppComplexType fieldType;
                    if (firstNonEmpty == ti) {
                        /* All base classes are empty, so this class forms the root of a new hierarchy.
                         * We have to be a little careful: the root-most class needs to have its alignment
                         * set to that of Il2CppObject, but we can't explicitly include Il2CppObject
                         * in the hierarchy because we want to customize the type of the klass parameter. */
                        var align = model.Package.BinaryImage.Bits == 32 ? 4 : 8;
                        fieldType = types.Struct(name + "__Fields", align);
                        GenerateFieldList(fieldType, ns, ti);
                    } else {
                        /* Include the base class fields. Alignment will be dictated by the hierarchy. */
                        ns.ReserveName("_");
                        fieldType = types.Struct(name + "__Fields");
                        var baseFieldType = types[TypeNamer.GetName(ti.BaseType) + "__Fields"];
                        fieldType.AddField("_", baseFieldType);
                        GenerateFieldList(fieldType, ns, ti);
                    }

                    var type = GenerateObjectStruct(name, ti);
                    types.AddField(type, "fields", name + "__Fields");
                    return (type, fieldType);
                }
            } else if (InheritanceStyle == CppCompilerType.GCC) {
                /* GCC style: after the base class, all fields in the hierarchy are concatenated.
                 * This saves space (fields are "packed") but requires us to repeat fields from
                 * base classes. */
                ns.ReserveName("klass");
                ns.ReserveName("monitor");

                var type = GenerateObjectStruct(name, ti);
                foreach (var bti in baseClasses)
                    GenerateFieldList(type, ns, bti);
                return (type, null);
            }
            throw new InvalidOperationException("Could not generate ref field struct");
        }

        // "Flush" the list of visited types, generating C structures for each one
        private List<(TypeInfo ilType, CppComplexType valueType, CppComplexType referenceType, CppComplexType fieldsType)> GenerateVisitedFieldStructs() {
            var structs = new List<(TypeInfo ilType, CppComplexType valueType, CppComplexType referenceType, CppComplexType fieldsType)>(TodoTypeStructs.Count);
            foreach (var ti in TodoFieldStructs) {
                if (ti.IsEnum || ti.IsValueType) {
                    var (valueType, boxedType) = GenerateValueFieldStruct(ti);
                    structs.Add((ti, valueType, boxedType, null));
                }
                else {
                    var (objectOrArrayType, fieldsType) = GenerateRefFieldStruct(ti);
                    structs.Add((ti, null, objectOrArrayType, fieldsType));
                }
            }
            TodoFieldStructs.Clear();
            return structs;
        }
        #endregion

        #region Class Struct Generation

        // Concrete implementations for abstract classes, for use in looking up VTable signatures and names
        private readonly Dictionary<TypeInfo, TypeInfo> ConcreteImplementations = new Dictionary<TypeInfo, TypeInfo>();
        /// <summary>
        /// VTables for abstract types have "null" in place of abstract functions.
        /// This function searches for concrete implementations so that we can properly
        /// populate the abstract class VTables.
        /// </summary>
        private void InitializeConcreteImplementations() {
            foreach (var ti in model.Types) {
                if (ti.HasElementType || ti.IsAbstract || ti.IsGenericParameter)
                    continue;
                var baseType = ti.BaseType;
                while (baseType != null) {
                    if (baseType.IsAbstract && !ConcreteImplementations.ContainsKey(baseType))
                        ConcreteImplementations[baseType] = ti;
                    baseType = baseType.BaseType;
                }
            }
        }

        /// <summary>
        /// Obtain the vtables for a given type, with implementations of abstract methods filled in.
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        private MethodBase[] GetFilledVTable(TypeInfo ti) {
            MethodBase[] res = ti.GetVTable();
            /* An abstract type will have null in the vtable for abstract methods.
             * In order to recover the correct method signature for such abstract
             * methods, we replace the corresponding vtable slot with an
             * implementation from a concrete subclass, as the name and signature
             * must match.
             * Note that, for the purposes of creating type structures, we don't
             * care which concrete implementation we put in this table! The name
             * and signature will always match that of the abstract type.
             */
            if (ti.IsAbstract && ConcreteImplementations.ContainsKey(ti)) {
                res = (MethodBase[])res.Clone();
                MethodBase[] impl = ConcreteImplementations[ti].GetVTable();
                for (int i = 0; i < res.Length; i++) {
                    if (res[i] == null)
                        res[i] = impl[i];
                }
            }
            return res;
        }

        private readonly HashSet<TypeInfo> VisitedTypes = new HashSet<TypeInfo>();
        private readonly List<TypeInfo> TodoTypeStructs = new List<TypeInfo>();

        /// <summary>
        /// Include the given type into this generator. This will add the given type and all types it depends on.
        /// Call GenerateRemainingTypeDeclarations to produce the actual type declarations afterwards.
        /// </summary>
        /// <param name="ti"></param>
        public void IncludeType(TypeInfo ti) {
            if (VisitedTypes.Contains(ti))
                return;
            if (ti.ContainsGenericParameters)
                return;
            VisitedTypes.Add(ti);

            if (ti.IsArray) {
                IncludeType(ti.ElementType);
            } else if (ti.HasElementType) {
                IncludeType(ti.ElementType);
            } else if (ti.IsEnum) {
                IncludeType(ti.GetEnumUnderlyingType());
            }

            // Visit all fields first, considering only value types,
            // so that we can get the layout correct.
            VisitFieldStructs(ti);

            if (ti.BaseType != null)
                IncludeType(ti.BaseType);

            TypeNamer.GetName(ti);

            foreach (var fi in ti.DeclaredFields)
                IncludeType(fi.FieldType);

            foreach (var mi in GetFilledVTable(ti))
                if (mi != null && !mi.ContainsGenericParameters)
                    IncludeMethod(mi);

            TodoTypeStructs.Add(ti);
        }

        // Generate the C structure for virtual function calls in a given type (the VTable)
        private CppComplexType GenerateVTableStruct(TypeInfo ti) {
            MethodBase[] vtable;
            if (ti.IsInterface) {
                /* Interface vtables are just all of the interface methods.
                   You might have to type a local variable manually as an
                   interface vtable during an interface call, but the result
                   should display the correct method name (with a computed
                   InterfaceOffset added).
                */
                vtable = ti.DeclaredMethods.ToArray();
            } else {
                vtable = ti.GetVTable();
            }
            var name = TypeNamer.GetName(ti);
            var namer = CreateNamespace().MakeNamer<int>((i) => vtable[i]?.Name?.ToCIdentifier() ?? "__unknown");

            // Il2Cpp switched to `VirtualInvokeData *vtable` in Unity 5.3.6.
            // Previous versions used `MethodInfo **vtable`.
            // TODO: Consider adding function types. This considerably increases the script size
            // but can significantly help with reverse-engineering certain binaries.
            var vtableStruct = types.Struct(name + "__VTable");

            if (UnityVersion.CompareTo("5.3.6") < 0) {
                for (int i = 0; i < vtable.Length; i++) {
                    types.AddField(vtableStruct, namer.GetName(i), "MethodInfo *");
                }
            } else {
                for (int i = 0; i < vtable.Length; i++) {
                    types.AddField(vtableStruct, namer.GetName(i), "VirtualInvokeData");
                }
            }
            return vtableStruct;
        }

        // Generate the overall Il2CppClass-shaped structure for the given type
        private (CppComplexType type, CppComplexType staticFields, CppComplexType vtable) GenerateTypeStruct(TypeInfo ti) {
            var name = TypeNamer.GetName(ti);
            var vtable = GenerateVTableStruct(ti);

            var statics = types.Struct(name + "__StaticFields");
            var namer = CreateNamespace().MakeNamer<FieldInfo>((field) => field.Name.ToCIdentifier());
            foreach (var field in ti.DeclaredFields) {
                if (field.IsLiteral || !field.IsStatic)
                    continue;
                statics.AddField(namer.GetName(field), AsCType(field.FieldType));
            }

            /* TODO: type the rgctx_data */
            var cls = types.Struct(name + "__Class");
            types.AddField(cls, "_0", "Il2CppClass_0");

            if (UnityVersion.CompareTo("5.5.0") < 0) {
                cls.AddField("vtable", vtable.AsPointer(types.WordSize));
                types.AddField(cls, "interfaceOffsets", "Il2CppRuntimeInterfaceOffsetPair *");
                cls.AddField("static_fields", statics.AsPointer(types.WordSize));
                types.AddField(cls, "rgctx_data", "Il2CppRGCTXData *", true);
                types.AddField(cls, "_1", "Il2CppClass_1");
            } else {
                types.AddField(cls, "interfaceOffsets", "Il2CppRuntimeInterfaceOffsetPair *");
                cls.AddField("static_fields", statics.AsPointer(types.WordSize));
                types.AddField(cls, "rgctx_data", "Il2CppRGCTXData *", true);
                types.AddField(cls, "_1", "Il2CppClass_1");
                cls.AddField("vtable", vtable);
            }
            return (cls, statics, vtable);
        }

        /// <summary>
        /// Output type declarations for every type that was included since the last call to GenerateRemainingTypeDeclarations
        /// Type declarations that have previously been generated by this instance of CppDeclarationGenerator will not be generated again.
        /// </summary>
        /// <returns>A string containing C type declarations</returns>
        public List<(TypeInfo ilType, CppComplexType valueType, CppComplexType referenceType, CppComplexType fieldsType,
            CppComplexType vtableType, CppComplexType staticsType)> GenerateRemainingTypeDeclarations() {
            var decl = GenerateVisitedFieldStructs().Select(s =>
                (s.ilType, s.valueType, s.referenceType, s.fieldsType, (CppComplexType) null, (CppComplexType) null)).ToList();

            foreach (var ti in TodoTypeStructs) {
                var (cls, statics, vtable) = GenerateTypeStruct(ti);
                decl.Add((ti, null, cls, null, vtable, statics));
            }
            TodoTypeStructs.Clear();

            return decl;
        }
        #endregion

        #region Method Generation

        /// <summary>
        /// Analyze a method and include all types that it takes and returns.
        /// Must call this before generating the method's declaration with GenerateMethodDeclaration or GenerateFunctionPointer.
        /// </summary>
        /// <param name="mi"></param>
        public void IncludeMethod(MethodBase method, TypeInfo declaringType = null) {
            if (!method.IsStatic)
                IncludeType(declaringType ?? method.DeclaringType);

            if (method is MethodInfo mi)
                IncludeType(mi.ReturnType);

            foreach (var pi in method.DeclaredParameters) {
                IncludeType(pi.ParameterType);
            }
        }

        // Generate a C declaration for a method
        private CppFnPtrType GenerateMethodDeclaration(MethodBase method, string name, TypeInfo declaringType) {
            CppType retType;
            if (method is MethodInfo mi) {
                retType = mi.ReturnType.FullName == "System.Void" ? types["void"] : AsCType(mi.ReturnType);
            } else {
                retType = types["void"];
            }

            var paramNs = CreateNamespace();
            paramNs.ReserveName("method");
            var paramNamer = paramNs.MakeNamer<ParameterInfo>((pi) => pi.Name == "" ? "arg" : pi.Name.ToCIdentifier());

            var paramList = new List<(string, CppType)>();
            // Figure out the "this" param
            if (method.IsStatic) {
                // In older versions, static methods took a dummy this parameter
                if (UnityVersion.CompareTo("2018.3.0") < 0)
                    paramList.Add(("this", types.GetType("void *")));
            } else {
                if (declaringType.IsValueType) {
                    // Methods for structs take the boxed object as the this param
                    paramList.Add(("this", types.GetType(TypeNamer.GetName(declaringType) + "__Boxed *")));
                } else {
                    paramList.Add(("this", AsCType(declaringType)));
                }
            }

            foreach (var pi in method.DeclaredParameters) {
                paramList.Add((paramNamer.GetName(pi), AsCType(pi.ParameterType)));
            }

            paramList.Add(("method", types.GetType("MethodInfo *")));

            return new CppFnPtrType(types.WordSize, retType, paramList) {Name = name};
        }

        /// <summary>
        /// Generate a declaration of the form "retType methName(argTypes argNames...)"
        /// You must first visit the method using VisitMethod and then call
        /// GenerateVisitedTypes in order to generate any dependent types.
        /// </summary>
        /// <param name="mi"></param>
        /// <returns></returns>
        public CppFnPtrType GenerateMethodDeclaration(MethodBase method) {
            return GenerateMethodDeclaration(method, GlobalNamer.GetName(method), method.DeclaringType);
        }
        #endregion

        #region Naming
        // We try decently hard to avoid creating clashing names, and also sanitize any invalid names.
        // You can customize how naming works by modifying this function.
        private void InitializeNaming() {
            TypeNamespace = CreateNamespace();
            TypeNamer = TypeNamespace.MakeNamer<TypeInfo>((ti) => {
                if (ti.IsArray)
                    return TypeNamer.GetName(ti.ElementType) + "__Array";
                var name = ti.Name.ToCIdentifier();
                name = Regex.Replace(name, "__+", "_");
                // Work around a dumb IDA bug: enums can't be named the same as certain "built-in" types
                // like KeyCode, Position, ErrorType. This only applies to enums, not structs.
                if (ti.IsEnum)
                    name += "__Enum";
                return name;
            });

            GlobalsNamespace = CreateNamespace();
            GlobalNamer = GlobalsNamespace.MakeNamer<MethodBase>((method) => $"{TypeNamer.GetName(method.DeclaringType)}_{method.Name.ToCIdentifier()}");
        }

        // Reserve C/C++ keywords and built-in names
        private static CppNamespace CreateNamespace() {
            var ns = new CppNamespace();
            /* Reserve C/C++ keywords */
            foreach (var keyword in new [] { "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic", "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local", "alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor", "bool", "break", "case", "catch", "char", "char16_t", "char32_t", "char8_t", "class", "co_await", "co_return", "co_yield", "compl", "concept", "const", "const_cast", "consteval", "constexpr", "constinit", "continue", "decltype", "default", "delete", "do", "double", "dynamic_cast", "else", "enum", "explicit", "export", "extern", "false", "final", "float", "for", "friend", "goto", "if", "inline", "int", "long", "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr", "operator", "or", "or_eq", "private", "protected", "public", "reflexpr", "register", "reinterpret_cast", "requires", "restrict", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast", "struct", "switch", "synchronized", "template", "this", "thread_local", "throw", "true", "try", "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq" }) {
                ns.ReserveName(keyword);
            }
            /* Reserve commonly defined C++ symbols for MSVC DLL projects */
            /* This is not an exhaustive list! (windows.h etc.) */
            foreach (var symbol in new[] {"_int32", "DEFAULT_CHARSET", "FILETIME", "NULL", "SYSTEMTIME", "stderr", "stdin", "stdout"}) {
                ns.ReserveName(symbol);
            }
            /* Reserve builtin keywords in IDA */
            foreach (var keyword in new [] {
                "_BYTE", "_DWORD", "_OWORD", "_QWORD", "_UNKNOWN", "_WORD",
                "__array_ptr", "__cdecl", "__cppobj", "__declspec", "__export", "__far", "__fastcall", "__hidden", "__huge", "__import",
                "__int128", "__int16", "__int32", "__int64", "__int8", "__interrupt", "__near", "__noreturn", "__pascal",
                "__ptr32", "__ptr64", "__pure", "__restrict", "__return_ptr", "__shifted", "__spoils", "__stdcall", "__struct_ptr",
                "__thiscall", "__thread", "__unaligned", "__usercall", "__userpurge",
                "_cs", "_ds", "_es", "_ss", "far", "flat", "near",
                "Mask", "Region", "Pointer", "GC" }) {
                ns.ReserveName(keyword);
            }
            /* Reserve builtin keywords for Ghidra */
            foreach (var keyword in new [] { "_extension" }) {
                ns.ReserveName(keyword);
            }
            return ns;
        }

        /// <summary>
        /// Namespace for all types and typedefs
        /// </summary>
        public CppNamespace TypeNamespace { get; private set; }

        public CppNamespace.Namer<TypeInfo> TypeNamer { get; private set; }

        /// <summary>
        /// Namespace for global variables and methods
        /// </summary>
        public CppNamespace GlobalsNamespace { get; private set; }
        public CppNamespace.Namer<MethodBase> GlobalNamer { get; private set; }
        #endregion
    }
}
