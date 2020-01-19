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
        private Il2CppModel model;

        public Il2CppIDAScriptDumper(Il2CppModel model) => this.model = model;

        #region Writing

        public void WriteScriptToFile(string outputFile) {
            using (var fs = new FileStream(outputFile, FileMode.Create))
            using (var sw = new StreamWriter(fs, Encoding.UTF8)) {
                writeSectionHeader(sw, "Preamble");
                writePreamble(sw);

                writeSectionHeader(sw, "Methods");
                writeMethods(sw, this.model.Types);

                writeSectionHeader(sw, "Usages");
                writeUsages(sw, this.model);
            }
        }

        private static void writePreamble(StreamWriter writer) {
            writeLines(writer,
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

        private static void writeMethods(StreamWriter writer, IEnumerable<TypeInfo> types) {
            foreach (var type in types.Where(t => t != null)) {
                writeMethods(writer, type.Name, type.DeclaredConstructors);
                writeMethods(writer, type.Name, type.DeclaredMethods);
            }
        }

        private static void writeMethods(StreamWriter writer, string typeName, IEnumerable<MethodBase> methods) {
            foreach (var method in methods) {
                if (!method.VirtualAddress.HasValue) continue;

                writeLines(writer,
                    $"SetName({method.VirtualAddress.Value.Start.ToAddressString()}, '{typeName}$${method.Name}')"
                );
            }
        }

        private static void writeUsages(StreamWriter writer, Il2CppModel model) {
            foreach (var usage in model.Package.MetadataUsages) {
                switch (usage.Type) {
                    case MetadataUsageType.TypeInfo:
                    case MetadataUsageType.Type:
                        var type = model.GetTypeFromUsage(usage.SourceIndex);
                        writeLines(writer,
                            $"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Class${type.Name}')"
                        );
                        break;
                    case MetadataUsageType.MethodDef:
                        var method = model.MethodsByDefinitionIndex[usage.SourceIndex];
                        writeLines(writer,
                            $"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Method${method.DeclaringType.Name}.{method.Name}')"
                        );
                        break;
                    case MetadataUsageType.FieldInfo:
                        var field = model.Package.Fields[usage.SourceIndex];
                        type = model.GetTypeFromUsage(field.typeIndex);
                        var fieldName = model.Package.Strings[field.nameIndex];
                        writeLines(writer,
                            $"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Field${type.Name}.{fieldName}')"
                        );
                        break;
                    case MetadataUsageType.StringLiteral:
                        // TODO: String literals
                        break;
                    case MetadataUsageType.MethodRef:
                        var methodSpec = model.Package.MethodSpecs[usage.SourceIndex];
                        var methodDef = model.MethodsByDefinitionIndex[methodSpec.methodDefinitionIndex];

                        var typeName = formatAsGeneric(methodDef.DeclaringType);
                        var methodName = formatAsGeneric(methodDef);
                        writeLines(writer,
                            $"SetName({model.Package.BinaryMetadataUsages[usage.DestinationIndex].ToAddressString()}, 'Method${typeName}.{methodName}')"
                        );
                        break;
                    default:
                        break;
                }
            }
        }

        private static void writeSectionHeader(StreamWriter writer, string sectionName) {
            writeLines(writer,
                $"# SECTION: {sectionName}",
                $"# -----------------------------"
            );
        }

        private static void writeLines(StreamWriter writer, params string[] lines) {
            foreach (var line in lines) {
                writer.WriteLine(line);
            }
        }

        #endregion

        #region Helpers

        private static string formatAsGeneric(TypeInfo type) {
            return formatAsGeneric(type, t => t.IsGenericType, t => t.Name, t => t.GenericTypeParameters);
        }

        private static string formatAsGeneric(MethodBase method) {
            return formatAsGeneric(method, m => m.IsGenericMethod, m => m.Name, m => m.GenericTypeParameters);
        }

        private static string formatAsGeneric<T>(T t, Func<T, bool> getIsGeneric, Func<T, string> getName, Func<T, List<TypeInfo>> getParams) {
            if (!getIsGeneric(t)) return getName(t);

            return $"{getName(t)}<{string.Join(", ", getParams(t).Select(tp => formatAsGeneric(tp)))}>";
        }

        #endregion
    }
}
