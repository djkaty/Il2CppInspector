/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Reflection;

namespace Il2CppInspector.Cpp
{
    // Class for generating C header declarations from Reflection objects (TypeInfo, etc.)
    public class CppDeclarationGenerator
    {
        private readonly Il2CppModel model;

        // Version number and header file to generate structures for
        public UnityVersion UnityVersion { get; }
        public UnityHeader UnityHeader { get; }

        // How inheritance of type structs should be represented.
        // Different C++ compilers lay out C++ class structures differently,
        // meaning that the compiler must be known in order to generate class type structures
        // with the correct layout.
        public enum InheritanceStyleEnum
        {
            C,      // Inheritance structs use C syntax, and will automatically choose MSVC or GCC based on inferred compiler.
            MSVC,   // Inheritance structs are laid out assuming the MSVC compiler, which recursively includes base classes
            GCC,    // Inheritance structs are laid out assuming the GCC compiler, which packs members from all bases + current class together
        }
        public InheritanceStyleEnum InheritanceStyle;

        public CppDeclarationGenerator(Il2CppModel model, UnityVersion version) {
            this.model = model;
            if (version == null) {
                UnityHeader = UnityHeader.GuessHeadersForModel(model)[0];
                UnityVersion = UnityHeader.MinVersion;
            } else {
                UnityVersion = version;
                UnityHeader = UnityHeader.GetHeaderForVersion(version);
                if (UnityHeader.MetadataVersion != model.Package.BinaryImage.Version) {
                    /* this can only happen in the CLI frontend with a manually-supplied version number */
                    Console.WriteLine($"Warning: selected version {UnityVersion} (metadata version {UnityHeader.MetadataVersion})" +
                        $" does not match metadata version {model.Package.BinaryImage.Version}.");
                }
            }

            InitializeNaming();
            InitializeConcreteImplementations();
        }

        private void GuessInheritanceStyle() {
            if (InheritanceStyle == InheritanceStyleEnum.C) {
                if (model.Package.BinaryImage is PEReader)
                    InheritanceStyle = InheritanceStyleEnum.MSVC;
                else
                    InheritanceStyle = InheritanceStyleEnum.GCC;
            }
        }

        // C type declaration used to name variables of the given C# type
        public string AsCType(TypeInfo ti) {
            // IsArray case handled by TypeNamer.GetName
            if (ti.IsByRef || ti.IsPointer) {
                return $"{AsCType(ti.ElementType)} *";
            } else if (ti.IsValueType) {
                if (ti.IsPrimitive) {
                    switch (ti.Name) {
                        case "Boolean": return "bool";
                        case "Byte": return "uint8_t";
                        case "SByte": return "int8_t";
                        case "Int16": return "int16_t";
                        case "UInt16": return "uint16_t";
                        case "Int32": return "int32_t";
                        case "UInt32": return "uint32_t";
                        case "Int64": return "int64_t";
                        case "UInt64": return "uint64_t";
                        case "IntPtr": return "void *";
                        case "UIntPtr": return "void *";
                        case "Char": return "uint16_t";
                        case "Double": return "double";
                        case "Single": return "float";
                    }
                }
                return $"struct {TypeNamer.GetName(ti)}";
            } else if (ti.IsEnum) {
                return $"enum {TypeNamer.GetName(ti)}";
            }
            return $"struct {TypeNamer.GetName(ti)} *";
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
            if (ti.IsByRef || ti.IsPointer || ti.ContainsGenericParameters)
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
        private void GenerateObjectFields(StringBuilder csrc, TypeInfo ti) {
            csrc.Append(
                $"  struct {TypeNamer.GetName(ti)}__Class *klass;\n" +
                $"  struct MonitorData *monitor;\n");
        }

        // Generate structure fields for each field of a given type
        private void GenerateFieldList(StringBuilder csrc, CppNamespace ns, TypeInfo ti) {
            var namer = ns.MakeNamer<FieldInfo>((field) => field.Name.ToCIdentifier());
            foreach (var field in ti.DeclaredFields) {
                if (field.IsLiteral || field.IsStatic)
                    continue;
                csrc.Append($"  {AsCType(field.FieldType)} {namer.GetName(field)};\n");
            }
        }

        // Generate the C structure for a value type, such as an enum or struct
        private void GenerateValueFieldStruct(StringBuilder csrc, TypeInfo ti) {
            string name = TypeNamer.GetName(ti);
            if (ti.IsEnum) {
                // Enums should be represented using enum syntax
                // They otherwise behave like value types
                csrc.Append($"enum {name} : {AsCType(ti.GetEnumUnderlyingType())} {{\n");
                foreach (var field in ti.DeclaredFields) {
                    if (field.Name != "value__")
                        csrc.Append($"  {EnumNamer.GetName(field)} = {field.DefaultValue},\n");
                }
                csrc.Append($"}};\n");

                // Use System.Enum base type as klass
                csrc.Append($"struct {name}__Boxed {{\n");
                GenerateObjectFields(csrc, ti.BaseType);
                csrc.Append($"  {AsCType(ti)} value;\n");
                csrc.Append($"}};\n");
            } else {
                // This structure is passed by value, so it doesn't include Il2CppObject fields.
                csrc.Append($"struct {name} {{\n");
                GenerateFieldList(csrc, CreateNamespace(), ti);
                csrc.Append($"}};\n");

                // Also generate the boxed form of the structure which includes the Il2CppObject header.
                csrc.Append($"struct {name}__Boxed {{\n");
                GenerateObjectFields(csrc, ti);
                csrc.Append($"  {AsCType(ti)} fields;\n");
                csrc.Append($"}};\n");
            }
        }

        // Generate the C structure for a reference type, such as a class or array
        private void GenerateRefFieldStruct(StringBuilder csrc, TypeInfo ti) {
            var name = TypeNamer.GetName(ti);
            if (ti.IsArray || ti.FullName == "System.Array") {
                var klassType = ti.IsArray ? ti : ti.BaseType;
                var elementType = ti.IsArray ? AsCType(ti.ElementType) : "void *";
                csrc.Append($"struct {name} {{\n");
                GenerateObjectFields(csrc, klassType);
                csrc.Append(
                    $"  struct Il2CppArrayBounds *bounds;\n" +
                    $"  il2cpp_array_size_t max_length;\n" +
                    $"  {elementType} vector[32];\n");
                csrc.Append($"}};\n");
                return;
            }

            if (InheritanceStyle == InheritanceStyleEnum.C)
                GuessInheritanceStyle();

            /* Generate a list of all base classes starting from the root */
            List<TypeInfo> baseClasses = new List<TypeInfo>();
            for (var bti = ti; bti != null; bti = bti.BaseType)
                baseClasses.Add(bti);
            baseClasses.Reverse();

            var ns = CreateNamespace();

            if (InheritanceStyle == InheritanceStyleEnum.MSVC) {
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
                    csrc.Append($"struct {name} {{\n");
                    GenerateObjectFields(csrc, ti);
                    csrc.Append($"}};\n");
                } else {
                    if (firstNonEmpty == ti) {
                        /* All base classes are empty, so this class forms the root of a new hierarchy.
                         * We have to be a little careful: the rootmost class needs to have its alignment
                         * set to that of Il2CppObject, but we can't explicitly include Il2CppObject
                         * in the hierarchy because we want to customize the type of the klass parameter. */
                        var align = model.Package.BinaryImage.Bits == 32 ? 4 : 8;
                        csrc.Append($"struct __declspec(align({align})) {name}__Fields {{\n");
                        GenerateFieldList(csrc, ns, ti);
                        csrc.Append($"}};\n");
                    } else {
                        /* Include the base class fields. Alignment will be dictated by the hierarchy. */
                        ns.ReserveName("_");
                        csrc.Append($"struct {name}__Fields {{\n");
                        csrc.Append($"  struct {TypeNamer.GetName(ti.BaseType)}__Fields _;\n");
                        GenerateFieldList(csrc, ns, ti);
                        csrc.Append($"}};\n");
                    }
                    csrc.Append($"struct {name} {{\n");
                    GenerateObjectFields(csrc, ti);
                    csrc.Append($"  struct {name}__Fields fields;\n");
                    csrc.Append($"}};\n");
                }
            } else if (InheritanceStyle == InheritanceStyleEnum.GCC) {
                /* GCC style: after the base class, all fields in the hierarchy are concatenated.
                 * This saves space (fields are "packed") but requires us to repeat fields from
                 * base classes. */
                ns.ReserveName("klass");
                ns.ReserveName("monitor");

                csrc.Append($"struct {name} {{\n");
                GenerateObjectFields(csrc, ti);
                foreach (var bti in baseClasses)
                    GenerateFieldList(csrc, ns, bti);
                csrc.Append($"}};\n");
            }
        }

        // "Flush" the list of visited types, generating C structures for each one
        private void GenerateVisitedFieldStructs(StringBuilder csrc) {
            foreach (var ti in TodoFieldStructs) {
                if (ti.IsEnum || ti.IsValueType)
                    GenerateValueFieldStruct(csrc, ti);
                else
                    GenerateRefFieldStruct(csrc, ti);
            }
            TodoFieldStructs.Clear();
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
                VisitFieldStructs(ti);
                IncludeType(ti.ElementType);
                IncludeType(ti.BaseType);
                return;
            } else if (ti.HasElementType) {
                IncludeType(ti.ElementType);
                return;
            } else if (ti.IsEnum) {
                VisitFieldStructs(ti);
                IncludeType(ti.GetEnumUnderlyingType());
                return;
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
        private void GenerateVTableStruct(StringBuilder csrc, TypeInfo ti) {
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
            csrc.Append($"struct {name}__VTable {{\n");
            if (UnityVersion.CompareTo("5.3.6") < 0) {
                for (int i = 0; i < vtable.Length; i++) {
                    csrc.Append($"  MethodInfo *{namer.GetName(i)};\n");
                }
            } else {
                for (int i = 0; i < vtable.Length; i++) {
                    csrc.Append($"  VirtualInvokeData {namer.GetName(i)};\n");
                }
            }
            csrc.Append($"}};\n");
        }

        // Generate the overall Il2CppClass-shaped structure for the given type
        private void GenerateTypeStruct(StringBuilder csrc, TypeInfo ti) {
            var name = TypeNamer.GetName(ti);
            GenerateVTableStruct(csrc, ti);

            csrc.Append($"struct {name}__StaticFields {{\n");
            var namer = CreateNamespace().MakeNamer<FieldInfo>((field) => field.Name.ToCIdentifier());
            foreach (var field in ti.DeclaredFields) {
                if (field.IsLiteral || !field.IsStatic)
                    continue;
                csrc.Append($"  {AsCType(field.FieldType)} {namer.GetName(field)};\n");
            }
            csrc.Append($"}};\n");

            /* TODO: type the rgctx_data */
            if (UnityVersion.CompareTo("5.5.0") < 0) {
                csrc.Append(
                    $"struct {name}__Class {{\n" +
                    $"  struct Il2CppClass_0 _0;\n" +
                    $"  struct {name}__VTable *vtable;\n" +
                    $"  Il2CppRuntimeInterfaceOffsetPair *interfaceOffsets;\n" +
                    $"  struct {name}__StaticFields *static_fields;\n" +
                    $"  const Il2CppRGCTXData *rgctx_data;\n" +
                    $"  struct Il2CppClass_1 _1;\n" +
                    $"}};\n");
            } else {
                csrc.Append(
                    $"struct {name}__Class {{\n" +
                    $"  struct Il2CppClass_0 _0;\n" +
                    $"  Il2CppRuntimeInterfaceOffsetPair *interfaceOffsets;\n" +
                    $"  struct {name}__StaticFields *static_fields;\n" +
                    $"  const Il2CppRGCTXData *rgctx_data;\n" +
                    $"  struct Il2CppClass_1 _1;\n" +
                    $"  struct {name}__VTable vtable;\n" +
                    $"}};\n");
            }
        }

        /// <summary>
        /// Output type declarations for every type that was included since the last call to GenerateRemainingTypeDeclarations
        /// Type declarations that have previously been generated by this instance of CppDeclarationGenerator will not be generated again.
        /// </summary>
        /// <returns>A string containing C type declarations</returns>
        public string GenerateRemainingTypeDeclarations() {
            var csrc = new StringBuilder();
            GenerateVisitedFieldStructs(csrc);

            foreach (var ti in TodoTypeStructs)
                GenerateTypeStruct(csrc, ti);
            TodoTypeStructs.Clear();

            return csrc.ToString();
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
        private string GenerateMethodDeclaration(MethodBase method, string name, TypeInfo declaringType) {
            string retType;
            if (method is MethodInfo mi) {
                retType = mi.ReturnType.FullName == "System.Void" ? "void" : AsCType(mi.ReturnType);
            } else {
                retType = "void";
            }

            var paramNs = CreateNamespace();
            paramNs.ReserveName("method");
            var paramNamer = paramNs.MakeNamer<ParameterInfo>((pi) => pi.Name == "" ? "arg" : pi.Name.ToCIdentifier());

            var paramList = new List<string>();
            // Figure out the "this" param
            if (method.IsStatic) {
                // In older versions, static methods took a dummy this parameter
                if (UnityVersion.CompareTo("2018.3.0") < 0)
                    paramList.Add("void *this");
            } else {
                if (declaringType.IsValueType) {
                    // Methods for structs take the boxed object as the this param
                    paramList.Add($"struct {TypeNamer.GetName(declaringType)}__Boxed * this");
                } else {
                    paramList.Add($"{AsCType(declaringType)} this");
                }
            }

            foreach (var pi in method.DeclaredParameters) {
                paramList.Add($"{AsCType(pi.ParameterType)} {paramNamer.GetName(pi)}");
            }

            paramList.Add($"struct MethodInfo *method");

            return $"{retType} {name}({string.Join(", ", paramList)})";
        }

        /// <summary>
        /// Generate a declaration of the form "retType methName(argTypes argNames...)"
        /// You must first visit the method using VisitMethod and then call
        /// GenerateVisitedTypes in order to generate any dependent types.
        /// </summary>
        /// <param name="mi"></param>
        /// <returns></returns>
        public string GenerateMethodDeclaration(MethodBase method) {
            return GenerateMethodDeclaration(method, GlobalNamer.GetName(method), method.DeclaringType);
        }

        /// <summary>
        /// Generate a declaration of the form "retType (*name)(argTypes...)"
        /// You must first visit the method using VisitMethod and then call
        /// GenerateVisitedTypes in order to generate any dependent types.
        /// </summary>
        /// <param name="mi">Method to generate (only the signature will be used)</param>
        /// <param name="name">Name of the function pointer</param>
        /// <returns></returns>
        public string GenerateFunctionPointer(MethodBase method, string name, TypeInfo declaringType = null) {
            return GenerateMethodDeclaration(method, $"(*{name})", declaringType ?? method.DeclaringType);
        }
        #endregion

        #region Naming
        // We try decently hard to avoid creating clashing names, and also sanitize any invalid names.
        // You can customize how naming works by modifying this function.
        private void InitializeNaming() {
            TypeNamespace = CreateNamespace();
            // Type names that may appear in the header
            foreach (var typeName in new string[] { "CustomAttributesCache", "CustomAttributeTypeCache", "EventInfo", "FieldInfo", "Hash16", "MemberInfo", "MethodInfo", "MethodVariableKind", "MonitorData", "ParameterInfo", "PInvokeArguments", "PropertyInfo", "SequencePointKind", "StackFrameType", "VirtualInvokeData" }) {
                TypeNamespace.ReserveName(typeName);
            }
            TypeNamer = TypeNamespace.MakeNamer<TypeInfo>((ti) => {
                if (ti.IsArray)
                    return TypeNamer.GetName(ti.ElementType) + "__Array";
                var name = ti.Name.ToCIdentifier();
                if (name.StartsWith("Il2Cpp"))
                    name = "_" + name;
                name = Regex.Replace(name, "__+", "_");
                // Work around a dumb IDA bug: enums can't be named the same as certain "built-in" types
                // like KeyCode, Position, ErrorType. This only applies to enums, not structs.
                if (ti.IsEnum)
                    name += "__Enum";
                return name;
            });

            GlobalsNamespace = CreateNamespace();
            GlobalNamer = GlobalsNamespace.MakeNamer<MethodBase>((method) => $"{TypeNamer.GetName(method.DeclaringType)}_{method.Name.ToCIdentifier()}");
            EnumNamer = GlobalsNamespace.MakeNamer<FieldInfo>((field) => $"{TypeNamer.GetName(field.DeclaringType)}_{field.Name.ToCIdentifier()}");
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
            foreach (var keyword in new [] { "_BYTE", "_DWORD", "_OWORD", "_QWORD", "_UNKNOWN", "_WORD", "__cdecl", "__declspec", "__export", "__far", "__fastcall", "__huge", "__import", "__int128", "__int16", "__int32", "__int64", "__int8", "__interrupt", "__near", "__pascal", "__spoils", "__stdcall", "__thiscall", "__thread", "__unaligned", "__usercall", "__userpurge", "_cs", "_ds", "_es", "_ss", "flat" }) {
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
        /// Namespace for global variables, enum values and methods
        /// </summary>
        public CppNamespace GlobalsNamespace { get; private set; }
        public CppNamespace.Namer<MethodBase> GlobalNamer { get; private set; }
        public CppNamespace.Namer<FieldInfo> EnumNamer { get; private set; }
        #endregion
    }
}
