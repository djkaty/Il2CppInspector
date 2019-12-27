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
                "#encoding: utf-8",
                "import idaapi",
                "",
                "def SetString(addr, comm):",
                "  global index",
                "  name = 'StringLiteral_' + str(index)",
                "  ret = idc.set_name(addr, name, SN_NOWARN)",
                "  idc.set_cmt(addr, comm, 1)",
                "",
                "def SetName(addr, name):",
                "  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)",
                "  if ret == 0:",
                "    new_name = name + '_' + str(addr)",
                "    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)",
                "",
                "index = 1",
                ""
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
                    $"SetName({toHex(method.VirtualAddress.Value.Start)}, '{typeName}$${method.Name}')"
                );
            }
        }

        private static void writeUsages(StreamWriter writer, Il2CppModel model) {
            var stringIndex = model.Package.Strings
                                .OrderBy(str => str.Key)
                                .Select(kvp => kvp.Value)
                                .ToList();

            var usages = BuildMetadataUsages(model.Package);
            foreach (var usage in usages) {
                switch (usage.Type) {
                    case MetadataUsageType.TypeInfo:
                    case MetadataUsageType.Type:
                        var type = model.GetTypeFromUsage(usage.SourceIndex);
                        writeLines(writer,
                            $"SetName({toHex(model.Package.MetadataUsages[usage.DestinationIndex])}, 'Class${type.Name}')"
                        );
                        break;
                    case MetadataUsageType.MethodDef:
                        var method = model.MethodsByDefinitionIndex[usage.SourceIndex];
                        writeLines(writer,
                            $"SetName({toHex(model.Package.MetadataUsages[usage.DestinationIndex])}, 'Method${method.DeclaringType.Name}.{method.Name}')"
                        );
                        break;
                    case MetadataUsageType.FieldInfo:
                        var field = model.Package.Fields[usage.SourceIndex];
                        type = model.GetTypeFromUsage(field.typeIndex);
                        var fieldName = model.Package.Strings[field.nameIndex];
                        writeLines(writer,
                            $"SetName({toHex(model.Package.MetadataUsages[usage.DestinationIndex])}, 'Field${type.Name}.{fieldName}')"
                        );
                        break;
                    case MetadataUsageType.StringLiteral:
                        // TODO this doesn't seem to be working as expected
                        //var str = stringIndex[usage.SourceIndex];
                        //if (usage.DestinationIndex >= model.Package.MetadataUsages.Length)
                        //{
                        //    Console.WriteLine($"WARNING: Destination Index out of bounds: {usage.DestinationIndex} ({str})");
                        //    break;
                        //}

                        //writeLines(writer,
                        //    $"SetString({toHex(model.Package.MetadataUsages[usage.DestinationIndex])}, r'{str}')"
                        //);
                        break;
                    case MetadataUsageType.MethodRef:
                        var methodSpec = model.Package.MethodSpecs[usage.SourceIndex];
                        var methodDef = model.MethodsByDefinitionIndex[methodSpec.methodDefinitionIndex];

                        var typeName = FormatAsGeneric(methodDef.DeclaringType);
                        var methodName = FormatAsGeneric(methodDef);
                        writeLines(writer,
                            $"SetName({toHex(model.Package.MetadataUsages[usage.DestinationIndex])}, 'Method${typeName}.{methodName}')"
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

        private static List<MetadataUsage> BuildMetadataUsages(Il2CppInspector package) {
            var metadataUsages = new Dictionary<uint, MetadataUsage>();
            foreach (var metadataUsageList in package.MetadataUsageLists) {
                for (var i = 0; i < metadataUsageList.count; i++) {
                    var metadataUsagePair = package.MetadataUsagePairs[metadataUsageList.start + i];
                    (var type, var sourceIndex) = DecodeEncodedSourceIndex(metadataUsagePair.encodedSourceIndex);
                    var destinationIndex = metadataUsagePair.destinationindex;

                    metadataUsages.TryAdd(destinationIndex, new MetadataUsage(type, (int)sourceIndex, (int)destinationIndex));
                }
            }

            return metadataUsages.Values.ToList();
        }

        private static string FormatAsGeneric(TypeInfo type) {
            return FormatAsGeneric(type, t => t.IsGenericType, t => t.Name, t => t.GenericTypeParameters);
        }

        private static string FormatAsGeneric(MethodBase method) {
            return FormatAsGeneric(method, m => m.IsGenericMethod, m => m.Name, m => m.GenericTypeParameters);
        }

        private static string FormatAsGeneric<T>(T t, Func<T, bool> getIsGeneric, Func<T, string> getName, Func<T, List<TypeInfo>> getParams) {
            if (!getIsGeneric(t)) return getName(t);

            return $"{getName(t)}<{string.Join(", ", getParams(t).Select(tp => FormatAsGeneric(tp)))}>";
        }


        private static string toHex(ulong l) {
            return $"0x{l.ToString("X")}";
        }

        private static (MetadataUsageType, uint) DecodeEncodedSourceIndex(uint srcIndex) {
            var encodedType = srcIndex & 0xE0000000;
            var methodIndex = srcIndex & 0x1FFFFFFF;

            var type = (MetadataUsageType)(encodedType >> 29);

            return (type, methodIndex);
        }

        #endregion

        #region Classes and enums

        public enum MetadataUsageType
        {
            TypeInfo = 1,
            Type = 2,
            MethodDef = 3,
            FieldInfo = 4,
            StringLiteral = 5,
            MethodRef = 6,
        }

        public class MetadataUsage
        {
            public MetadataUsageType Type { get; }
            public int SourceIndex { get; }
            public int DestinationIndex { get; }

            public MetadataUsage(MetadataUsageType type, int sourceIndex, int destinationIndex)
            {
                Type = type;
                SourceIndex = sourceIndex;
                DestinationIndex = destinationIndex;
            }
        }

        #endregion
    }
}
