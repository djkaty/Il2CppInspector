// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using Il2CppInspector.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Outputs
{
    /// <summary>
    /// A utility class for automatically resolving name clashes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class UniqueRenamer<T>
    {
        private Dictionary<T, string> names = new Dictionary<T, string>();
        private Dictionary<string, int> renameCount = new Dictionary<string, int>();
        public delegate string KeyFunc(T t);
        private KeyFunc keyFunc;

        public UniqueRenamer(KeyFunc keyFunc) {
            this.keyFunc = keyFunc;
        }

        public void ReserveName(string name) {
            if (renameCount.ContainsKey(name)) {
                throw new Exception($"Can't reserve {name}: already taken!");
            }
            renameCount[name] = 0;
        }

        public string GetName(T t) {
            string name;
            if (names.TryGetValue(t, out name))
                return name;
            name = keyFunc(t);
            // This approach avoids linear scan (quadratic blowup) if there are a lot of similarly-named objects.
            if (renameCount.ContainsKey(name)) {
                int v = renameCount[name] + 1;
                while (renameCount.ContainsKey(name + "_" + v))
                    v++;
                renameCount[name] = v;
                name = name + "_" + v;
            }
            renameCount[name] = 0;
            names[t] = name;
            return name;
        }
    }

    public class IDAPythonScript
    {
        private readonly Il2CppModel model;
        private StreamWriter writer;

        public IDAPythonScript(Il2CppModel model) => this.model = model;

        public void WriteScriptToFile(string outputFile) {
            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("# Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("print('Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty')");

            writeSectionHeader("Preamble");
            writePreamble();

            writeMethods();

            writeSectionHeader("Metadata Usages");
            writeUsages();

            writeSectionHeader("Function boundaries");
            writeFunctions();

            writeSectionHeader("IL2CPP Metadata");
            writeMetadata();

            writeSectionHeader("Object Types");
            writeObjectTypes();

            writeLine("print('Script execution complete.')");
            writer.Close();
        }

        private void writePreamble() {
            writeLine(
@"import idaapi

def SetName(addr, name):
  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
  if ret == 0:
    new_name = name + '_' + str(addr)
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)"
            );
        }

        private void writeMethods() {
            writeSectionHeader("Method definitions");
            foreach (var type in model.Types) {
                writeMethods(type.Name, type.DeclaredConstructors);
                writeMethods(type.Name, type.DeclaredMethods);
            }

            writeSectionHeader("Constructed generic methods");
            foreach(var method in model.MethodsByReferenceIndex) {
                if (!method.VirtualAddress.HasValue)
                    continue;
                var address = method.VirtualAddress.Value.Start;
                writeName(address, $"{method.DeclaringType.Name}_{method.Name}{method.GetFullTypeParametersString()}");
                writeComment(address, method);
            }

            writeSectionHeader("Custom attributes generators");
            foreach (var method in model.AttributesByIndices.Values.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start;
                writeName(address, $"{method.AttributeType.Name}_CustomAttributesCacheGenerator");
                writeComment(address, $"{method.AttributeType.Name}_CustomAttributesCacheGenerator(CustomAttributesCache *)");
            }

            writeSectionHeader("Method.Invoke thunks");
            foreach (var method in model.MethodInvokers.Where(m => m != null)) {
                var address = method.VirtualAddress.Start;
                writeName(address, method.Name);
                writeComment(address, method);
            }
        }

        private static string sanitizeIdentifier(string id) {
            return Regex.Replace(id, "[^a-zA-Z0-9_]", "_");
        }

        private void writeMethods(string typeName, IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start;
                writeName(address, $"{typeName}_{method.Name}");
                writeComment(address, method);
            }
        }

        private void writeUsages() {
            if (model.Package.MetadataUsages == null) {
                /* Version < 19 calls `il2cpp_codegen_string_literal_from_index` to get string literals.
                 * Unfortunately, metadata references are just loose globals in Il2CppMetadataUsage.cpp
                 * so we can't automatically name those. Next best thing is to define an enum for the strings. */
                var enumSrc = new StringBuilder();
                enumSrc.Append("enum StringLiteralIndex {\n");
                for (int i = 0; i < model.Package.StringLiterals.Length; i++) {
                    var str = model.Package.StringLiterals[i];
                    str = str.Substring(0, Math.Min(32, str.Length));
                    enumSrc.Append($"  STRINGLITERAL_{i}_{sanitizeIdentifier(str)},\n");
                }
                enumSrc.Append("};\n");

                writeDecls(enumSrc.ToString());

                return;
            }

            var usageNamer = new UniqueRenamer<MetadataUsage>((usage) => sanitizeIdentifier(model.GetMetadataUsageName(usage)));
            var stringNamer = new UniqueRenamer<MetadataUsage>((usage) => {
                var str = model.GetMetadataUsageName(usage);
                return sanitizeIdentifier(str.Substring(0, Math.Min(32, str.Length)));
            });
            foreach (var usage in model.Package.MetadataUsages) {
                var address = usage.VirtualAddress;

                if (usage.Type == MetadataUsageType.StringLiteral) {
                    writeName(address, "StringLiteral_" + stringNamer.GetName(usage));
                    var str = model.GetMetadataUsageName(usage);
                    writeComment(address, str);
                } else {
                    writeName(address, usageNamer.GetName(usage) + "_" + usage.Type);
                    if (usage.Type == MetadataUsageType.MethodDef || usage.Type == MetadataUsageType.MethodRef) {
                        var method = model.GetMetadataUsageMethod(usage);
                        writeComment(address, method);
                    } else {
                        var type = model.GetMetadataUsageType(usage);
                        writeComment(address, type);
                    }
                }
            }
        }

        private void writeFunctions() {
            foreach (var func in model.Package.FunctionAddresses)
                if (func.Key != func.Value)
                    writeLine($"idc.MakeFunction({func.Key.ToAddressString()})");
        }

        private void writeMetadata() {
            var binary = model.Package.Binary;

            // TODO: In the future, add struct definitions/fields, data ranges and the entire IL2CPP metadata tree

            writeName(binary.CodeRegistrationPointer, "g_CodeRegistration");
            writeName(binary.MetadataRegistrationPointer, "g_MetadataRegistration");

            if (model.Package.Version >= 24.2)
                writeName(binary.CodeRegistration.pcodeGenModules, "g_CodeGenModules");

            foreach (var ptr in binary.CodeGenModulePointers)
                writeName(ptr.Value, $"g_{ptr.Key.Replace(".dll", "")}CodeGenModule");
            
            // This will be zero if we found the structs from the symbol table
            if (binary.RegistrationFunctionPointer != 0)
                writeName(binary.RegistrationFunctionPointer, "__GLOBAL__sub_I_Il2CppCodeRegistration.cpp");
        }

        private UniqueRenamer<TypeInfo> TypeNamer = new UniqueRenamer<TypeInfo>((ti) => {
            var name = sanitizeIdentifier(ti.Name);
            if (name.StartsWith("Il2Cpp"))
                name = "_" + name;
            return name;
        });
        private HashSet<TypeInfo> GeneratedTypes = new HashSet<TypeInfo>();
        private Dictionary<TypeInfo, TypeInfo> ConcreteImplementations = new Dictionary<TypeInfo, TypeInfo>();
        private HashSet<TypeInfo> EmptyTypes = new HashSet<TypeInfo>();

        /// <summary>
        /// VTables for abstract types have "null" in place of abstract functions.
        /// This function searches for concrete implementations so that we can properly
        /// populate the abstract class VTables.
        /// </summary>
        private void populateConcreteImplementations() {
            foreach(var ti in model.Types) {
                if (ti.HasElementType || ti.IsAbstract)
                    continue;
                var baseType = ti.BaseType;
                while(baseType != null) {
                    if (baseType.IsAbstract && !ConcreteImplementations.ContainsKey(baseType))
                        ConcreteImplementations[baseType] = ti;
                    baseType = baseType.BaseType;
                }
            }
        }

        /// <summary>
        /// Obtain the vtables for a given type.
        /// </summary>
        /// <param name="ti"></param>
        /// <returns></returns>
        private MethodBase[] getVTable(TypeInfo ti) {
            MethodBase[] res = model.GetVTable(ti);
            /* An abstract type will have null in the vtable for abstract methods.
             * In order to recover the correct method signature for such abstract
             * methods, we replace the corresponding vtable slot with an
             * implementation from *any* concrete subclass, as the name and signature
             * must match.
             */
            if (ti.IsAbstract && ConcreteImplementations.ContainsKey(ti)) {
                res = (MethodBase[])res.Clone();
                MethodBase[] impl = model.GetVTable(ConcreteImplementations[ti]);
                for (int i = 0; i < res.Length; i++) {
                    if (res[i] == null)
                        res[i] = impl[i];
                }
            }
            return res;
        }

        private string getCType(TypeInfo ti) {
            if (ti.IsArray) {
                return $"struct {TypeNamer.GetName(ti.ElementType)}__Array *";
            } else if (ti.IsByRef || ti.IsPointer) {
                return $"{getCType(ti.ElementType)} *";
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
                        case "Decimal": return "__int128";
                        case "Double": return "double";
                        case "Single": return "float";
                    }
                }
                return $"struct {TypeNamer.GetName(ti)}";
            } else if(ti.IsEnum) {
                return $"enum {TypeNamer.GetName(ti)}";
            }
            return $"struct {TypeNamer.GetName(ti)} *";
        }

        private Queue<TypeInfo> typesToGenerate = new Queue<TypeInfo>();

        private void generateFieldStructsForType(StringBuilder csrc, TypeInfo ti) {
            string cName = TypeNamer.GetName(ti);
            int special = 0;
            if (ti.Namespace == "System" && ti.BaseName == "Array") {
                /* System.Array is special - instances are Il2CppArray* */
                csrc.Append($"struct {cName}__Fields {{\n");
                csrc.Append($"  struct Il2CppArrayBounds *bounds;\n" +
                            $"  size_t max_length;\n" +
                            $"  void *elems[32];\n");
                csrc.Append("};\n");
                special = 1;
            } else if(ti.IsEnum) {
                csrc.Append($"enum {cName} : {getCType(ti.GetEnumUnderlyingType())} {{\n");
                csrc.Append(string.Join(",\n", ti.GetEnumNames().Zip(ti.GetEnumValues().OfType<object>(),
                             (k, v) => new { k, v }).OrderBy(x => x.v).Select(x => $"  {cName}_{x.k} = {x.v}")) + "\n");
                csrc.Append("};\n");
                special = 1;
            }

            /* Walk the fields twice and generate field definitions */
            for (int i = special; i < 2; i++) {
                bool isStatic = (i == 1);
                /* Generate any dependent types */
                foreach (var field in ti.DeclaredFields.Where((x) => (x.IsStatic == isStatic))) {
                    var fti = field.FieldType;
                    // TODO: handle generics properly
                    if (!fti.ContainsGenericParameters)
                        generateOrDefer(csrc, fti);
                }

                //model.Package.BinaryImage is PEReader;

                var fieldNamer = new UniqueRenamer<FieldInfo>((field) => sanitizeIdentifier(field.Name));
                if (isStatic)
                    csrc.Append($"struct {cName}__StaticFields {{\n");
                else if (ti.IsValueType)
                    csrc.Append($"struct {cName} {{\n");
                else
                    csrc.Append($"struct {cName}__Fields {{\n");

                bool empty = true;
                foreach (var field in ti.DeclaredFields.Where((x) => (x.IsStatic == isStatic))) {
                    string name = fieldNamer.GetName(field);
                    var fti = field.FieldType;
                    if (fti.ContainsGenericParameters) {
                        /* TODO: Handle generic parameters properly! */
                        csrc.Append($"  void *{name};\n");
                    } else {
                        csrc.Append($"  {getCType(fti)} {name};\n");
                    }
                    empty = false;
                }
                if (empty && !isStatic)
                    EmptyTypes.Add(ti);

                csrc.Append("};\n");
            }
        }

        // Generate structures for value types, and defer generating structures
        // for reference types until later. This avoids type-referential cycles.
        private void generateOrDefer(StringBuilder csrc, TypeInfo ti) {
            if (ti.IsValueType || ti.IsEnum)
                generateStructsForType(csrc, ti);
            else if (!GeneratedTypes.Contains(ti))
                typesToGenerate.Enqueue(ti);
        }

        private void generateStructsForType(StringBuilder csrc, TypeInfo ti) {
            if (GeneratedTypes.Contains(ti))
                return;

            GeneratedTypes.Add(ti);
            if (ti.IsArray) {
                generateStructsForType(csrc, ti.ElementType);
                generateStructsForType(csrc, ti.BaseType);
                csrc.Append($"struct {TypeNamer.GetName(ti.ElementType)}__Array {{\n" +
                    $"  struct {TypeNamer.GetName(ti.BaseType)}__Class *klass;\n" +
                    $"  void *monitor;\n" +
                    $"  struct Il2CppArrayBounds *bounds;\n" +
                    $"  size_t max_length;\n" +
                    $"  {getCType(ti.ElementType)} elems[32];\n" +
                    $"}};\n");
                return;
            } else if (ti.IsByRef || ti.IsPointer) {
                return;
            }

            if (ti.BaseType != null)
                generateStructsForType(csrc, ti.BaseType);

            generateFieldStructsForType(csrc, ti);

            string vtableEntryType;
            if(model.Package.Version < 21) {
                vtableEntryType = "MethodInfo *";
            } else {
                /* TODO: Metadata version 21 might be MethodInfo * or VirtualInvokeData
                 * depending on the exact version of Unity - we need a way to distinguish
                 * these cases... */
                vtableEntryType = "VirtualInvokeData";
            }
            string cName = TypeNamer.GetName(ti);
            csrc.Append($"struct {cName}__VTable {{\n");
            if (ti.IsInterface) {
                /* Interface vtables are just all of the interface methods.
                   You might have to type a local variable manually as an
                   interface pointer during an interface call, but the result
                   should display the correct method name (with a computed
                   InterfaceOffset added).
                */
                var funcNamer = new UniqueRenamer<MethodInfo>((mi) => sanitizeIdentifier(mi.Name));
                foreach (var mi in ti.DeclaredMethods) {
                    csrc.Append($"  {vtableEntryType} {funcNamer.GetName(mi)};\n");
                }
            } else {
                var vtable = getVTable(ti);
                var funcNamer = new UniqueRenamer<int>((i) => sanitizeIdentifier(vtable[i].Name));
                for (int i = 0; i < vtable.Length; i++) {
                    var mi = vtable[i];
                    /* TODO type the functions correctly */
                    if (mi == null)
                        csrc.Append($"  {vtableEntryType} __unknown_{i};\n");
                    else
                        csrc.Append($"  {vtableEntryType} {funcNamer.GetName(i)};\n");
                }
            }
            csrc.Append($"}};\n");

            /* TODO: type the rgctx_data */
            if(model.Package.Version < 22) {
                csrc.Append($"struct {cName}__Class {{\n" +
                    $"  struct Il2CppClass_0 _0;\n" +
                    $"  struct {cName}__VTable *vtable;\n" +
                    $"  Il2CppRuntimeInterfaceOffsetPair *interfaceOffsets;\n" +
                    $"  struct {cName}__StaticFields *static_fields;\n" +
                    $"  void **rgctx_data;\n" +
                    $"  struct Il2CppClass_1 _1;\n" +
                    $"}};\n");
            } else {
                csrc.Append($"struct {cName}__Class {{\n" +
                    $"  struct Il2CppClass_0 _0;\n" +
                    $"  Il2CppRuntimeInterfaceOffsetPair *interfaceOffsets;\n" +
                    $"  struct {cName}__StaticFields *static_fields;\n" +
                    $"  void **rgctx_data;\n" +
                    $"  struct Il2CppClass_1 _1;\n" +
                    $"  struct {cName}__VTable vtable;\n" +
                    $"}};\n");
            }

            /* For value types, __Object is rarely used. It seems to only be used
             * when a struct is passed via this to an instance method.
             * Hence, we use __Object instead of the plain name, since the plain
             * name will be used for the (much more common) fields instead. */
            if (ti.IsValueType || ti.IsEnum) {
                csrc.Append($"struct {cName}__Object {{\n");
            } else {
                csrc.Append($"struct {cName} {{\n");
            }
            csrc.Append(
                $"  struct {cName}__Class *klass;\n" +
                $"  void *monitor;\n");
            addBaseClassFields(ti.BaseType, csrc);
            if (!EmptyTypes.Contains(ti)) {
                if (ti.IsValueType) {
                    csrc.Append($"  struct {cName} fields;\n");
                } else if (ti.IsEnum) {
                    csrc.Append($"  enum {cName} value;\n");
                } else {
                    csrc.Append($"  struct {cName}__Fields fields;\n");
                }
            }
            csrc.Append($"}};\n");
        }

        private int addBaseClassFields(TypeInfo ti, StringBuilder csrc) {
            if (ti == null)
                return 0;
            int res = addBaseClassFields(ti.BaseType, csrc);
            if (!EmptyTypes.Contains(ti))
                csrc.Append($"  struct {TypeNamer.GetName(ti)}__Fields base{res};\n");
            return res + 1;
        }

        private void writeObjectTypes() {
            // Compatibility (in a separate decl block in case these are already defined)
            writeDecls(@"
typedef unsigned __int8 uint8_t;
typedef unsigned __int16 uint16_t;
typedef unsigned __int32 uint32_t;
typedef unsigned __int64 uint64_t;
typedef __int8 int8_t;
typedef __int16 int16_t;
typedef __int32 int32_t;
typedef __int64 int64_t;
");

            string header = "";
            if(model.Package.BinaryImage.Bits == 32) {
                header += "#define IS_32BIT\n";
            }
            if(model.Package.Version == 16) {
                header += Data.ResourceReader.ReadFileAsString("16-5.3.0f4.i");
            } else if(model.Package.Version == 19) {
                header += Data.ResourceReader.ReadFileAsString("19-5.3.2f1.i");
            } else if (model.Package.Version == 20) {
                header += Data.ResourceReader.ReadFileAsString("20-5.3.3f1.i");
            } else if (model.Package.Version == 21) {
                /* XXX TODO: 5.3.5f1 and 5.4.0f3 differ in one critical respect:
                 * the vtable in 5.3.5 is MethodInfo**, but VirtualInvokeData* in 5.4.0.
                 * Don't know how to distinguish these two versions right now (they're both
                 * metadata version 21).
                 */
                header += Data.ResourceReader.ReadFileAsString("21-5.4.0f3.i");
            } else if (model.Package.Version == 22) {
                header += Data.ResourceReader.ReadFileAsString("22-5.5.0f3.i");
            } else if (model.Package.Version == 23) {
                header += Data.ResourceReader.ReadFileAsString("23-5.6.2p3.i");
            } else if (model.Package.Version == 24.0) {
                header += Data.ResourceReader.ReadFileAsString("24.0-2017.2f3.i");
            } else if (model.Package.Version == 24.1) {
                header += Data.ResourceReader.ReadFileAsString("24.1-2018.3.0f2.i");
            } else if (model.Package.Version == 24.2) {
                /* no significant differences between 2019.2.8f1 and 2019.3.1f1 */
                header += Data.ResourceReader.ReadFileAsString("24.2-2019.3.1f1.i");
            } else {
                writeLine($"print('Metadata version {model.Package.Version} is not fully supported! Types are not available.')");
                return;
            }
            writeDecls(header);

            /* Type names which may appear in the header */
            TypeNamer.ReserveName("CustomAttributesCache");
            TypeNamer.ReserveName("CustomAttributeTypeCache");
            TypeNamer.ReserveName("EventInfo");
            TypeNamer.ReserveName("FieldInfo");
            TypeNamer.ReserveName("MethodInfo");
            TypeNamer.ReserveName("MethodVariableKind");
            TypeNamer.ReserveName("NativeObject");
            TypeNamer.ReserveName("ParameterInfo");
            TypeNamer.ReserveName("PInvokeArguments");
            TypeNamer.ReserveName("PropertyInfo");
            TypeNamer.ReserveName("SequencePointKind");
            TypeNamer.ReserveName("signscale");
            TypeNamer.ReserveName("StackFrameType");
            TypeNamer.ReserveName("VirtualInvokeData");

            /* Other type names that may be predefined in IDA (not a complete list - add an entry if IDA complains about types being defined already) */
            TypeNamer.ReserveName("KeyCode");

            /* Find concrete implementations of abstract classes so that vtables can be filled out properly */
            populateConcreteImplementations();

            /* Set the type of all TypeInfo structures, thus resolving static field references */
            if (model.Package.MetadataUsages == null) {
                /* Version < 19 calls `il2cpp_codegen_type_info_from_index` to get TypeInfo references.
                 * Unfortunately, metadata references are just loose globals in Il2CppMetadataUsage.cpp
                 * so we can't automatically type them. Next best thing is to define an enum for the types */
                var typeSrc = new StringBuilder();
                var enumSrc = new StringBuilder();
                enumSrc.Append("enum TypeIndex {\n");
                for (int i = 0; i < model.TypesByReferenceIndex.Length; i++) {
                    var ti = model.TypesByReferenceIndex[i];
                    if (!ti.ContainsGenericParameters)
                        generateStructsForType(typeSrc, ti);
                    enumSrc.Append($"  TYPEREF_{i}_{TypeNamer.GetName(ti)},\n");
                }
                enumSrc.Append("};\n");

                if (typeSrc.Length > 0)
                    writeDecls(typeSrc.ToString());
                writeDecls(enumSrc.ToString());
            } else {
                foreach (var usage in model.Package.MetadataUsages) {
                    if (usage.Type != MetadataUsageType.TypeInfo && usage.Type != MetadataUsageType.Type)
                        continue;
                    var ti = model.GetMetadataUsageType(usage);
                    if (ti.HasElementType)
                        continue;
                    var csrc = new StringBuilder();
                    generateStructsForType(csrc, ti);
                    if (csrc.Length > 0)
                        writeDecls(csrc.ToString());
                    var address = usage.VirtualAddress;
                    writeType(address, $"{TypeNamer.GetName(ti)}__Class *");
                    // Rename to match the type name exactly
                    writeName(address, $"{TypeNamer.GetName(ti)}_TypeInfo");
                }
            }

            writeSectionHeader("Function types");
            foreach (var ti in model.Types) {
                writeMethodTypes(ti, ti.DeclaredConstructors);
                writeMethodTypes(ti, ti.DeclaredMethods);
            }

            while(typesToGenerate.TryDequeue(out TypeInfo ti)) {
                var csrc = new StringBuilder();
                generateStructsForType(csrc, ti);
                if (csrc.Length > 0)
                    writeDecls(csrc.ToString());
            }
        }

        private void writeMethodTypes(TypeInfo ti, IEnumerable<MethodBase> methods) {
            var typeName = TypeNamer.GetName(ti);
            var funcNamer = new UniqueRenamer<MethodBase>((method) => $"{typeName}_{sanitizeIdentifier(method.Name)}");
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue && !m.ContainsGenericParameters)) {
                var address = method.VirtualAddress.Value.Start;
                writeName(address, funcNamer.GetName(method));

                var csrc = new StringBuilder();
                MethodInfo mi = method as MethodInfo;
                string retType;
                if (mi == null || mi.ReturnType.FullName == "System.Void") {
                    retType = "void";
                } else {
                    generateOrDefer(csrc, mi.ReturnType);
                    retType = getCType(mi.ReturnType);
                }

                var paramNamer = new UniqueRenamer<ParameterInfo>((param) => (param.Name == "" || param.Name == "this") ? "arg" : sanitizeIdentifier(param.Name));
                var parms = new List<string>();
                if(!method.IsStatic) {
                    generateOrDefer(csrc, ti);
                    if(ti.IsValueType && !ti.HasElementType) {
                        parms.Add($"{getCType(ti)}__Object * this");
                    } else {
                        parms.Add($"{getCType(ti)} this");
                    }
                }

                foreach(var param in method.DeclaredParameters) {
                    generateOrDefer(csrc, param.ParameterType);
                    parms.Add($"{getCType(param.ParameterType)} {paramNamer.GetName(param)}");
                }

                if (csrc.Length > 0)
                    writeDecls(csrc.ToString());
                writeType(address, $"{retType} f({string.Join(", ", parms)})");
            }
        }


        private void writeSectionHeader(string sectionName) {
            writeLine("");
            writeLine($"# SECTION: {sectionName}"); 
            writeLine($"# -----------------------------");
            writeLine($"print('Processing {sectionName}')");
            writeLine("");
        }

        private void writeDecls(string decls) {
            var lines = decls.Replace("\r", "").Split('\n');
            var cleanLines = lines.Select((s) => s.ToEscapedString());
            writeLine("idc.parse_decls('''" + string.Join('\n', cleanLines) + "''')");
        }

        private void writeType(ulong address, string type) {
            writeLine($"SetType({address.ToAddressString()}, r'{type.ToEscapedString()}')");
        }

        private void writeName(ulong address, string name) {
            writeLine($"SetName({address.ToAddressString()}, r'{name.ToEscapedString()}')");
        }

        private void writeComment(ulong address, object comment) {
            writeLine($"idc.set_cmt({address.ToAddressString()}, r'{comment.ToString().ToEscapedString()}', 1)");
        }

        private void writeLine(string line) => writer.WriteLine(line);
    }
}
