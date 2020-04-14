// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Il2CppInspector.Reflection;
using Il2CppInspector.Outputs.UnityHeaders;

namespace Il2CppInspector.Outputs
{
    public class IDAPythonScript
    {
        private readonly Il2CppModel model;
        private StreamWriter writer;
        public UnityVersion UnityVersion;
        private UnityHeader header;

        public IDAPythonScript(Il2CppModel model) => this.model = model;

        public void WriteScriptToFile(string outputFile) {
            if (UnityVersion == null) {
                header = UnityHeader.GuessHeadersForModel(model)[0];
                UnityVersion = header.MinVersion;
            } else {
                header = UnityHeader.GetHeaderForVersion(UnityVersion);
                if (header.MetadataVersion != model.Package.BinaryImage.Version) {
                    /* this can only happen in the CLI frontend with a manually-supplied version number */
                    Console.WriteLine($"Warning: selected version {UnityVersion} (metadata version {header.MetadataVersion}) does not match metadata version {model.Package.BinaryImage.Version}.");
                }
            }

            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeLine("# Generated script file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty");
            writeLine("# Target Unity version: " + header);
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

            var prefix = (model.Package.BinaryImage.Bits == 32) ? "#define IS_32BIT\n" : "";
            writeDecls(prefix + header.GetHeaderText());
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

        private void writeMethods(string typeName, IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start;
                writeName(address, $"{typeName}_{method.Name}");
                writeComment(address, method);
            }
        }

        private void writeUsages() {
            if (model.Package.MetadataUsages == null) {
                /* Version < 19 - no MetadataUsages table */
                return;
            }

            foreach (var usage in model.Package.MetadataUsages) {
                var address = usage.VirtualAddress;
                var name = model.GetMetadataUsageName(usage);

                if (usage.Type != MetadataUsageType.StringLiteral)
                    writeName(address, $"{name}_{usage.Type}");
                else
                    writeString(address, name);

                if (usage.Type == MetadataUsageType.MethodDef || usage.Type == MetadataUsageType.MethodRef) {
                    var method = model.GetMetadataUsageMethod(usage);
                    writeComment(address, method);
                } else if (usage.Type != MetadataUsageType.StringLiteral) {
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

        private void writeString(ulong address, string str) {
            writeLine($"SetString({address.ToAddressString()}, r'{str.ToEscapedString()}')");
        }

        private void writeComment(ulong address, object comment) {
            writeLine($"idc.set_cmt({address.ToAddressString()}, r'{comment.ToString().ToEscapedString()}', 1)");
        }

        private void writeLine(string line) => writer.WriteLine(line);
    }
}
