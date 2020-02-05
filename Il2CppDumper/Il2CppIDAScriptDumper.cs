// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Il2CppInspector.Reflection;

namespace Il2CppInspector
{
    public class Il2CppIDAScriptDumper
    {
        private readonly Dictionary<MetadataUsageType, string> usagePrefixes = new Dictionary<MetadataUsageType, string> {
            [MetadataUsageType.TypeInfo] = "Class",
            [MetadataUsageType.Type] = "Class",
            [MetadataUsageType.MethodDef] = "Method",
            [MetadataUsageType.FieldInfo] = "Field",
            [MetadataUsageType.StringLiteral] = "String",
            [MetadataUsageType.MethodRef] = "Method"
        };

        private readonly Il2CppModel model;
        private StreamWriter writer;

        public Il2CppIDAScriptDumper(Il2CppModel model) => this.model = model;

        public void WriteScriptToFile(string outputFile) {
            using var fs = new FileStream(outputFile, FileMode.Create);
            writer = new StreamWriter(fs, Encoding.UTF8);

            writeSectionHeader("Preamble");
            writePreamble();

            writeSectionHeader("Methods");
            writeMethods();

            writeSectionHeader("Usages");
            writeUsages();

            writer.Close();
        }

        private void writePreamble() {
            writeLines(
@"#encoding: utf-8
import idaapi

def SetString(addr, comm):
  name = 'StringLiteral_' + str(addr)
  ret = idc.set_name(addr, name, SN_NOWARN)
  idc.set_cmt(addr, comm, 1)

def SetName(addr, name):
  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
  if ret == 0:
    new_name = name + '_' + str(addr)
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)
"
            );
        }

        private void writeMethods() {
            foreach (var type in model.Types) {
                writeMethods(type.Name, type.DeclaredConstructors);
                writeMethods(type.Name, type.DeclaredMethods);
            }

            foreach (var method in model.GenericMethods.Values.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start.ToAddressString();
                writeLines($"SetName({address}, '{method.DeclaringType.Name}$${method.Name}{method.GetFullTypeParametersString()}')");
                writeLines($"idc.set_cmt({address}, r'{method}', 1)");
            }

            foreach (var method in model.AttributesByIndices.Values.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start.ToAddressString();
                writeLines($"SetName({address}, 'CustomAttributesGenerator${method.AttributeType.FullName}')");
                writeLines($"idc.set_cmt({address}, r'{method}', 1)");
            }

            foreach (var method in model.MethodInvokers.Where(m => m != null)) {
                var address = method.VirtualAddress.Start.ToAddressString();
                writeLines($"SetName({address}, '{method.Name}')");
                writeLines($"idc.set_cmt({address}, r'{method}', 1)");
            }
        }

        private void writeMethods(string typeName, IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                var address = method.VirtualAddress.Value.Start.ToAddressString();
                writeLines($"SetName({address}, '{typeName}$${method.Name}')");
                writeLines($"idc.set_cmt({address}, r'{method}', 1)");
            }
        }

        private void writeUsages() {
            foreach (var usage in model.Package.MetadataUsages) {
                var address = usage.VirtualAddress.ToAddressString();
                var escapedName = model.GetMetadataUsageName(usage).ToEscapedString();

                if (usage.Type != MetadataUsageType.StringLiteral)
                    writeLines($"SetName({address}, '{usagePrefixes[usage.Type]}${escapedName}')");
                else
                    writeLines($"SetString({address}, r'{escapedName}')");

                if (usage.Type == MetadataUsageType.MethodDef || usage.Type == MetadataUsageType.MethodRef) {
                    var method = model.GetMetadataUsageMethod(usage);
                    writeLines($"idc.set_cmt({address}, r'{method}', 1)");
                }
                else if (usage.Type != MetadataUsageType.StringLiteral) {
                    var type = model.GetMetadataUsageType(usage);
                    writeLines($"idc.set_cmt({address}, r'{type}', 1)");
                }
            }
        }

        private void writeSectionHeader(string sectionName) {
            writeLines(
                $"# SECTION: {sectionName}",
                $"# -----------------------------"
            );
        }

        private void writeLines(params string[] lines) {
            foreach (var line in lines) {
                writer.WriteLine(line);
            }
        }
    }
}
