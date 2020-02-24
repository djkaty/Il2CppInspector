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
            } else {
                renameCount[name] = 0;
            }
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

def SetString(addr, comm):
  name = 'StringLiteral_' + str(addr)
  ret = idc.set_name(addr, name, SN_NOWARN)
  idc.set_cmt(addr, comm, 1)

def SetName(addr, name):
  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
  if ret == 0:
    new_name = name + '_' + str(addr)
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)

def MakeFunction(start, end):
  next_func = idc.get_next_func(start)
  if next_func < end:
    end = next_func
  current_func = idaapi.get_func(start)
  if current_func is not None and current_func.startEA == start:
    ida_funcs.del_func(start)
  ida_funcs.add_func(start, end)"
            );
        }

        private void writeMethods() {
            writeSectionHeader("Method definitions");
            foreach (var type in model.Types) {
                writeMethods(type.Name, type.DeclaredConstructors);
                writeMethods(type.Name, type.DeclaredMethods);
            }

            writeSectionHeader("Constructed generic methods");
            foreach (var method in model.GenericMethods.Values.Where(m => m.VirtualAddress.HasValue)) {
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
            var usageNamer = new UniqueRenamer<MetadataUsage>((usage) => sanitizeIdentifier($"{model.GetMetadataUsageName(usage)}"));
            foreach (var usage in model.Package.MetadataUsages) {
                var address = usage.VirtualAddress;
                var name = model.GetMetadataUsageName(usage);

                if (usage.Type != MetadataUsageType.StringLiteral)
                    writeName(address, usageNamer.GetName(usage) + "_" + usage.Type);
                else
                    writeString(address, name);

                if (usage.Type == MetadataUsageType.MethodDef || usage.Type == MetadataUsageType.MethodRef) {
                    var method = model.GetMetadataUsageMethod(usage);
                    writeComment(address, method);
                }
                else if (usage.Type != MetadataUsageType.StringLiteral) {
                    var type = model.GetMetadataUsageType(usage);
                    writeComment(address, type);
                }
            }
        }

        private void writeFunctions() {
            foreach (var func in model.Package.FunctionAddresses)
                if (func.Key != func.Value)
                    writeLine($"MakeFunction({func.Key.ToAddressString()}, {func.Value.ToAddressString()})");
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

        private UniqueRenamer<TypeInfo> TypeNamer = new UniqueRenamer<TypeInfo>((ti) => sanitizeIdentifier(ti.Name));
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
            MethodBase[] res = new MethodBase[ti.Definition.vtable_count];
            MethodBase[] impl = null;
            if(ti.IsAbstract && ConcreteImplementations.ContainsKey(ti)) {
                impl = getVTable(ConcreteImplementations[ti]);
            }
            for (int i = 0; i < ti.Definition.vtable_count; i++) {
                // XXX TODO: Resolve generic methods if parameters are known
                var encodedIndex = model.Package.VTableMethodIndices[ti.Definition.vtableStart + i];
                var encodedType = encodedIndex & 0xE0000000;
                var usageType = (MetadataUsageType)(encodedType >> 29);
                var index = encodedIndex & 0x1FFFFFFF;
                if (index == 0) {
                    if (impl != null)
                        res[i] = impl[i];
                    else
                        res[i] = null;
                } else if (usageType == MetadataUsageType.MethodRef) {
                    res[i] = model.MethodsByDefinitionIndex[model.Package.MethodSpecs[index].methodDefinitionIndex];
                } else {
                    res[i] = model.MethodsByDefinitionIndex[index];
                }
            }
            return res;
        }

        private string getCType(TypeInfo ti) {
            if (ti.IsArray) {
                return $"struct {TypeNamer.GetName(ti.ElementType)}__Array *";
            } else if (ti.IsByRef || ti.IsPointer) {
                return $"{getCType(ti.ElementType)} *";
            }
            if (ti.IsValueType) {
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
            }
            return $"struct {TypeNamer.GetName(ti)} *";
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
                    $"  struct __Il2CppArrayBounds *bounds;\n" +
                    $"  size_t max_length;\n" +
                    $"  {getCType(ti.ElementType)} elems[1];\n" +
                    $"}};\n");
                return;
            } else if (ti.IsByRef || ti.IsPointer) {
                return;
            }

            if (ti.BaseType != null)
                generateStructsForType(csrc, ti.BaseType);

            /* Walk the fields twice and generate field definitions */
            string cName = TypeNamer.GetName(ti);
            for (int i = 0; i < 2; i++) {
                bool isStatic = (i == 1);
                /* Generate any dependent types */
                foreach (var field in ti.DeclaredFields.Where((x) => (x.IsStatic == isStatic))) {
                    var fti = field.FieldType;
                    // TODO: handle generics properly
                    if (!fti.ContainsGenericParameters)
                        generateStructsForType(csrc, fti);
                }

                var fieldNamer = new UniqueRenamer<FieldInfo>((field) => sanitizeIdentifier(field.Name));
                if (isStatic)
                    csrc.Append($"struct {cName}__StaticFields {{\n");
                else if (ti.IsValueType)
                    csrc.Append($"struct {cName} {{\n");
                else
                    csrc.Append($"struct {cName}__Fields {{\n");

                bool empty = true;
                foreach (var field in ti.DeclaredFields.Where((x) => (x.IsStatic == isStatic)).OrderBy((x) => x.Offset)) {
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
                    csrc.Append($"  __VirtualInvokeData {funcNamer.GetName(mi)};\n");
                }
            } else {
                var vtable = getVTable(ti);
                var funcNamer = new UniqueRenamer<int>((i) => sanitizeIdentifier(vtable[i].Name));
                for (int i = 0; i < vtable.Length; i++) {
                    var mi = vtable[i];
                    /* TODO type the functions correctly */
                    if (mi == null)
                        csrc.Append($"  __VirtualInvokeData __unknown_{i};\n");
                    else
                        csrc.Append($"  __VirtualInvokeData {funcNamer.GetName(i)};\n");
                }
            }
            csrc.Append($"}};\n");

            csrc.Append($"struct {cName}__Class {{\n" +
                $"  struct __Il2CppClass_1 _1;\n" +
                $"  struct {cName}__StaticFields *static_fields;\n" +
                $"  struct __Il2CppClass_2 _2;\n" +
                $"  struct {cName}__VTable vtable;\n" +
                $"}};\n");

            /* For value types, __Object is rarely used. It seems to only be used
             * when a struct is passed via this to an instance method.
             * Hence, we use __Object instead of the plain name, since the plain
             * name will be used for the (much more common) fields instead. */
            if (ti.IsValueType) {
                csrc.Append($"struct {cName}__Object {{\n");
            } else {
                csrc.Append($"struct {cName} {{\n");
            }
            csrc.Append(
                $"  struct {cName}__Class *klass;\n" +
                $"  void *monitor;\n");
            addBaseClassFields(ti.BaseType, csrc);
            if (!EmptyTypes.Contains(ti)) {
                if(ti.IsValueType) {
                    csrc.Append($"  struct {cName} fields;\n");
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

            // TODO: Add support for the class structures for more versions
            if (model.Package.Version == 24.0) {
                writeDecls(@"
struct __Il2CppArrayBounds {
    size_t length;
    int32_t lower_bound;
};

struct __VirtualInvokeData {
    void *methodPtr;
    const void *method;
};

struct __Il2CppRuntimeInterfaceOffsetPair {
    struct __Il2CppClass* interfaceType;
    int32_t offset;
};

struct __Il2CppClass_1 {
  const struct __Il2CppImage *image;
  void *gc_desc;
  const char *name;
  const char *namespaze;
  struct __Il2CppType *byval_arg;
  struct __Il2CppType *this_arg;
  struct __Il2CppClass *element_class;
  struct __Il2CppClass *castClass;
  struct __Il2CppClass *declaringType;
  struct __Il2CppClass *parent;
  struct __Il2CppGenericClass *generic_class;
  const struct __Il2CppTypeDefinition *typeDefinition;
  const struct __Il2CppInteropData *interopData;
  struct __FieldInfo *fields;
  const struct __EventInfo *events;
  const struct __PropertyInfo *properties;
  const struct __MethodInfo **methods;
  struct __Il2CppClass **nestedTypes;
  struct __Il2CppClass **implementedInterfaces;
  struct __Il2CppRuntimeInterfaceOffsetPair *interfaceOffsets;
};
  /* static_fields */
struct __Il2CppClass_2 {
  const struct __Il2CppRGCTXData *rgctx_data;
  struct __Il2CppClass **typeHierarchy;
  uint32_t cctor_started;
  uint32_t cctor_finished;
  /* 8-byte-aligned 4-byte field, requiring 4 bytes of padding on 32-bit */
  uint32_t cctor_thread_lo;
  uint32_t cctor_thread_hi;
  int genericContainerIndex;
  int customAttributeIndex;
  uint32_t instance_size;
  uint32_t actualSize;
  uint32_t element_size;
  int32_t native_size;
  uint32_t static_fields_size;
  uint32_t thread_static_fields_size;
  int32_t thread_static_fields_offset;
  uint32_t flags;
  uint32_t token;
  uint16_t method_count;
  uint16_t property_count;
  uint16_t field_count;
  uint16_t event_count;
  uint16_t nested_type_count;
  uint16_t vtable_count;
  uint16_t interfaces_count;
  uint16_t interface_offsets_count;
  uint8_t typeHierarchyDepth;
  uint8_t genericRecursionDepth;
  uint8_t rank;
  uint8_t minimumAlignment;
  uint8_t packingSize;
  uint8_t __bitflags1;
  uint8_t __bitflags2;
};
/* vtable */

/* generic class structure */
struct __Il2CppClass {
    struct __Il2CppClass_1 _1;
    void *static_fields;
    struct __Il2CppClass_2 _2;
};
");
            } else if(model.Package.Version == 24.2) {
                writeDecls(@"
struct __Il2CppArrayBounds {
    size_t length;
    int32_t lower_bound;
};

struct __VirtualInvokeData {
    void *methodPtr;
    const void *method;
};

struct __Il2CppRuntimeInterfaceOffsetPair {
    struct __Il2CppClass* interfaceType;
    int32_t offset;
};

struct __Il2CppType {
  void *data;
  uint32_t flags;
};

struct __Il2CppClass_1 {
  const struct __Il2CppImage* image;
  void* gc_desc;
  const char* name;
  const char* namespaze;
  struct __Il2CppType byval_arg;
  struct __Il2CppType this_arg;
  struct __Il2CppClass* element_class;
  struct __Il2CppClass* castClass;
  struct __Il2CppClass* declaringType;
  struct __Il2CppClass* parent;
  struct __Il2CppGenericClass *generic_class;
  const struct __Il2CppTypeDefinition* typeDefinition;
  const struct __Il2CppInteropData* interopData;
  struct __Il2CppClass* klass;

  struct __FieldInfo* fields;
  const struct __EventInfo* events;
  const struct __PropertyInfo* properties;
  const struct __MethodInfo** methods;
  struct __Il2CppClass** nestedTypes;
  struct __Il2CppClass** implementedInterfaces;
  struct __Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets;
};
/* static_fields */
struct __Il2CppClass_2 {
  const struct __Il2CppRGCTXData* rgctx_data;
  struct __Il2CppClass** typeHierarchy;

  void *unity_user_data;
  uint32_t initializationExceptionGCHandle;

  uint32_t cctor_started;
  uint32_t cctor_finished;
  /* 8-byte-aligned 4-byte field, but no padding required on 32-bit due to positioning */
  size_t cctor_thread;

  int32_t genericContainerIndex;
  uint32_t instance_size;
  uint32_t actualSize;
  uint32_t element_size;
  int32_t native_size;
  uint32_t static_fields_size;
  uint32_t thread_static_fields_size;
  int32_t thread_static_fields_offset;
  uint32_t flags;
  uint32_t token;

  uint16_t method_count;
  uint16_t property_count;
  uint16_t field_count;
  uint16_t event_count;
  uint16_t nested_type_count;
  uint16_t vtable_count;
  uint16_t interfaces_count;
  uint16_t interface_offsets_count;

  uint8_t typeHierarchyDepth;
  uint8_t genericRecursionDepth;
  uint8_t rank;
  uint8_t minimumAlignment;
  uint8_t naturalAligment;
  uint8_t packingSize;

  uint8_t __bitflags1;
  uint8_t __bitflags2;
};
/* vtable */

/* generic class structure */
struct __Il2CppClass {
    struct __Il2CppClass_1 _1;
    void *static_fields;
    struct __Il2CppClass_2 _2;
};
");
            } else {
                throw new Exception("don't know struct layout for metadata version " + model.Package.Version);
            }

            /* Find concrete implementations of abstract classes so that vtables can be filled out properly */
            populateConcreteImplementations();

            /* Set the type of all TypeInfo structures, thus resolving static field references */
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

            writeSectionHeader("Function types");
            foreach (var ti in model.Types) {
                writeMethodTypes(ti, ti.DeclaredConstructors);
                writeMethodTypes(ti, ti.DeclaredMethods);
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
                    generateStructsForType(csrc, mi.ReturnType);
                    retType = getCType(mi.ReturnType);
                }

                var paramNamer = new UniqueRenamer<ParameterInfo>((param) => (param.Name == "" || param.Name == "this") ? "arg" : sanitizeIdentifier(param.Name));
                var parms = new List<string>();
                if(!method.IsStatic) {
                    generateStructsForType(csrc, ti);
                    if(ti.IsValueType && !ti.HasElementType) {
                        parms.Add($"{getCType(ti)}__Object * this");
                    } else {
                        parms.Add($"{getCType(ti)} this");
                    }
                }

                foreach(var param in method.DeclaredParameters) {
                    generateStructsForType(csrc, param.ParameterType);
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

        private void writeString(ulong address, string str) {
            writeLine($"SetString({address.ToAddressString()}, r'{str.ToEscapedString()}')");
        }

        private void writeComment(ulong address, object comment) {
            writeLine($"idc.set_cmt({address.ToAddressString()}, r'{comment.ToString().ToEscapedString()}', 1)");
        }

        private void writeLine(string line) => writer.WriteLine(line);
    }
}
