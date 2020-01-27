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
            foreach (var type in model.Types.Where(t => t != null)) {
                writeMethods(type.Name, type.DeclaredConstructors);
                writeMethods(type.Name, type.DeclaredMethods);
            }
        }

        private void writeMethods(string typeName, IEnumerable<MethodBase> methods) {
            foreach (var method in methods.Where(m => m.VirtualAddress.HasValue)) {
                writeLines($"SetName({method.VirtualAddress.Value.Start.ToAddressString()}, '{typeName}$${method.Name}')");
            }
        }

        private void writeUsages() {
            foreach (var usage in model.Package.MetadataUsages) {
                var escapedName = model.GetMetadataUsageName(usage).ToEscapedString();
                var address = model.Package.BinaryMetadataUsages[usage.DestinationIndex];

                if (usage.Type != MetadataUsageType.StringLiteral)
                    writeLines($"SetName({address.ToAddressString()}, '{usagePrefixes[usage.Type]}${escapedName}')");
                else
                    writeLines($"SetString({address.ToAddressString()}, r'{escapedName}')");
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
