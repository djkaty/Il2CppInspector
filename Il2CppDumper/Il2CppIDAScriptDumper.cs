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

            writeSectionHeader( "Usages");
            writeUsages();
        }

        private void writePreamble() {
            writeLines(
@"#encoding: utf-8
import idaapi

def SetString(addr, comm):
  global index
  name = 'StringLiteral_' + str(index)
  ret = idc.set_name(addr, name, SN_NOWARN)
  idc.set_cmt(addr, comm, 1)

def SetName(addr, name):
  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)
  if ret == 0:
    new_name = name + '_' + str(addr)
    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)

index = 1
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
                switch (usage.Type) {
                    case MetadataUsageType.TypeInfo:
                    case MetadataUsageType.Type:
                        var type = model.GetTypeFromUsage(usage.SourceIndex);
                        writeLines($"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Class${type.Name}')");
                        break;
                    case MetadataUsageType.MethodDef:
                        var method = model.MethodsByDefinitionIndex[usage.SourceIndex];
                        writeLines($"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Method${method.DeclaringType.Name}.{method.Name}')");
                        break;
                    case MetadataUsageType.FieldInfo:
                        var field = model.Package.Fields[usage.SourceIndex];
                        type = model.GetTypeFromUsage(field.typeIndex);
                        var fieldName = model.Package.Strings[field.nameIndex];
                        writeLines($"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Field${type.Name}.{fieldName}')");
                        break;
                    case MetadataUsageType.StringLiteral:
                        // TODO: String literals
                        break;
                    case MetadataUsageType.MethodRef:
                        var methodSpec = model.Package.MethodSpecs[usage.SourceIndex];
                        method = model.MethodsByDefinitionIndex[methodSpec.methodDefinitionIndex];
                        type = method.DeclaringType;
                        writeLines($"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Method${type.Name}.{method.Name}')");
                        break;
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
