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

        public void WriteScriptToFile(string outputFile) {
            using (var fs = new FileStream(outputFile, FileMode.Create))
            using (var sw = new StreamWriter(fs, Encoding.UTF8)) {
                writePreamble(sw);
                writeMethods(sw);
            }
        }

        private void writePreamble(StreamWriter writer) {
            writeLines(writer,
                "#encoding: utf-8",
                "import idaapi",
                "",
                "def SetString(addr, comm):",
                "  global index",
                "  name = 'StringLiteral_' + str(index)",
                "  ret = idc.set_name(addr, name, SN_NOWARN)",
                "  idc.set_cmt(addr, comm, 1)",
                "  index += 1",
                "",
                "def SetName(addr, name):",
                "  ret = idc.set_name(addr, name, SN_NOWARN | SN_NOCHECK)",
                "  if ret == 0:",
                "    new_name = name + '_' + str(addr)",
                "    ret = idc.set_name(addr, new_name, SN_NOWARN | SN_NOCHECK)",
                "",
                "def MakeFunction(start, end):",
                "  next_func = idc.get_next_func(start)",
                "  if next_func < end:",
                "    end = next_func",
                "  if idc.get_func_attr(start, FUNCATTR_START) == start:",
                "    ida_funcs.del_func(start)",
                "  ida_funcs.add_func(start, end)",
                "",
                "index = 1",
                ""
            );
        }

        private void writeMethods(StreamWriter writer) {
            foreach (var type in this.model.Types.Where(t => t != null)) {
                foreach (var method in type.DeclaredMethods) {
                    if (!method.VirtualAddress.HasValue) continue;

                    writeLines(writer,
                        $"SetName({toHex(method.VirtualAddress.Value.Start)}, '{type.Name}$${method.Name}')"
                    );
                }
            }
        }

        private void writeLines(StreamWriter writer, params string[] lines) {
            foreach (var line in lines) {
                writer.WriteLine(line);
            }
        }

        private string toHex(ulong l) {
            return $"0x{l.ToString("X")}";
        }
    }
}
