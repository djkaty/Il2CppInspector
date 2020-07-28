// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Il2CppInspector.Reflection;
using Il2CppInspector.Cpp;
using Il2CppInspector.Model;

namespace Il2CppInspector.Outputs
{
    public class IDAPythonScript
    {
        private readonly AppModel model;
        private StreamWriter writer;

        public IDAPythonScript(AppModel model) => this.model = model;

        public void WriteScriptToFile(string outputFile) {

            // Write types file first
            var typeHeaderFile = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + ".h");
            writeTypes(typeHeaderFile);

            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("# Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("# Target Unity version: " + model.UnityHeaders);
            writeLine("print('Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty')");
            writeSectionHeader("Preamble");
            writePreamble();

            writeSectionHeader("Types");
            writeLine(
@"original_macros = ida_typeinf.get_c_macros()
ida_typeinf.set_c_macros(original_macros + "";_IDA_=1"")
idc.parse_decls(""" + Path.GetFileName(typeHeaderFile) + @""", idc.PT_FILE)
ida_typeinf.set_c_macros(original_macros)");

            writeMethods();

            writeSectionHeader("String literals");
            writeStringLiterals();

            writeUsages();

            writeSectionHeader("Function boundaries");
            writeFunctions();

            writeSectionHeader("IL2CPP Metadata");
            writeMetadata();

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
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)

def MakeFunction(start):
  ida_funcs.add_func(start)

def SetType(addr, type):
  ret = idc.SetType(addr, type)
  if ret is None:
    print('SetType(0x%x, %r) failed!' % (addr, type))");
        }

        private void writeTypes(string typeHeaderFile) {
            var cpp = new CppScaffolding(model);
            cpp.WriteTypes(typeHeaderFile);
        }

        private void writeMethods() {
            writeSectionHeader("Method definitions");
            writeMethods(model.GetMethodGroup("types_from_methods"));

            writeSectionHeader("Constructed generic methods");
            writeMethods(model.GetMethodGroup("types_from_generic_methods"));

            writeSectionHeader("Custom attributes generators");
            foreach (var method in model.ILModel.AttributesByIndices.Values) {
                writeTypedName(method.VirtualAddress.Value.Start, method.Signature, method.Name);
            }

            writeSectionHeader("Method.Invoke thunks");
            foreach (var method in model.ILModel.MethodInvokers.Where(m => m != null)) {
                writeTypedName(method.VirtualAddress.Start, method.GetSignature(model.UnityVersion), method.Name);
            }
        }

        private void writeMethods(IEnumerable<AppMethod> methods) {
            foreach (var method in methods) {
                writeTypedName(method.MethodCodeAddress, method.CppFnPtrType.ToSignatureString(), method.CppFnPtrType.Name);
                writeComment(method.MethodCodeAddress, method.Method);
            }
        }

        private void writeStringLiterals() {
            // For version < 19
            if (model.StringIndexesAreOrdinals) {
                var enumSrc = new StringBuilder();
                enumSrc.Append("enum StringLiteralIndex {\n");
                foreach (var str in model.Strings)
                    enumSrc.Append($"  STRINGLITERAL_{str.Key}_{stringToIdentifier(str.Value)},\n");
                enumSrc.Append("};\n");

                writeDecls(enumSrc.ToString());
                return;
            }

            // For version >= 19
            var stringType = model.CppTypeCollection.GetType("String *");

            foreach (var str in model.Strings) {
                writeTypedName(str.Key, stringType.ToString(), $"StringLiteral_{stringToIdentifier(str.Value)}");
                writeComment(str.Key, str.Value);
            }
        }

        private void writeUsages() {
            // Definition and reference addresses for all types from metadata usages
            writeSectionHeader("Il2CppClass (TypeInfo) and Il2CppType (TypeRef) pointers");

            foreach (var type in model.Types.Values) {
                // A type may have no addresses, for example an unreferenced array type

                if (type.TypeClassAddress != 0xffffffff_ffffffff) {
                    writeTypedName(type.TypeClassAddress, $"struct {type.Name}__Class *", $"{type.Name}__TypeInfo");
                    writeComment(type.TypeClassAddress, type.ILType.CSharpName);
                }

                if (type.TypeRefPtrAddress != 0xffffffff_ffffffff) {
                    // A generic type definition does not have any direct C++ types, but may have a reference
                    writeTypedName(type.TypeRefPtrAddress, "struct Il2CppType *", $"{type.Name}__TypeRef");
                    writeComment(type.TypeRefPtrAddress, type.ILType.CSharpName);
                }
            }

            // Metedata usage methods
            writeSectionHeader("MethodInfo pointers");

            foreach (var method in model.Methods.Values.Where(m => m.MethodInfoPtrAddress != 0xffffffff_ffffffff)) {
                writeTypedName(method.MethodInfoPtrAddress, "struct MethodInfo *", $"{method.CppFnPtrType.Name}__MethodInfo");
                writeComment(method.MethodInfoPtrAddress, method.Method);
            }
        }

        private void writeFunctions() {
            foreach (var func in model.Package.FunctionAddresses)
                writeLine($"MakeFunction({func.Key.ToAddressString()})");
        }

        private void writeMetadata() {
            var binary = model.Package.Binary;

            // TODO: In the future, add struct definitions/fields, data ranges and the entire IL2CPP metadata tree

            writeTypedName(binary.CodeRegistrationPointer, "struct Il2CppCodeRegistration", "g_CodeRegistration");
            writeTypedName(binary.MetadataRegistrationPointer, "struct Il2CppMetadataRegistration", "g_MetadataRegistration");

            if (model.Package.Version >= 24.2)
                writeTypedName(binary.CodeRegistration.pcodeGenModules,
                    $"struct Il2CppCodeGenModule *[{binary.CodeRegistration.codeGenModulesCount}]", "g_CodeGenModules");

            foreach (var ptr in binary.CodeGenModulePointers)
                writeTypedName(ptr.Value, "struct Il2CppCodeGenModule", $"g_{ptr.Key.Replace(".dll", "")}CodeGenModule");

            // This will be zero if we found the structs from the symbol table
            if (binary.RegistrationFunctionPointer != 0)
                writeName(binary.RegistrationFunctionPointer, "__GLOBAL__sub_I_Il2CppCodeRegistration.cpp");
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
            var declString = string.Join('\n', cleanLines);
            if (declString != "")
                writeLine("idc.parse_decls('''" + declString + "''')");
        }

        private void writeName(ulong address, string name) {
            writeLine($"SetName({address.ToAddressString()}, r'{name.ToEscapedString()}')");
        }

        private void writeTypedName(ulong address, string type, string name) {
            writeName(address, name);
            writeLine($"SetType({address.ToAddressString()}, r'{type.ToEscapedString()}')");
        }

        private void writeComment(ulong address, object comment) {
            writeLine($"idc.set_cmt({address.ToAddressString()}, r'{comment.ToString().ToEscapedString()}', 1)");
        }

        private void writeLine(string line) => writer.WriteLine(line);

        private static string stringToIdentifier(string str) {
            str = str.Substring(0, Math.Min(32, str.Length));
            return str.ToCIdentifier();
        }
    }
}
