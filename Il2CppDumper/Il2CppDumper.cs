// Copyright (c) 2017 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System.IO;
using System.Linq;
using Il2CppInspector.Reflection;

namespace Il2CppInspector
{
    public class Il2CppDumper
    {
        private readonly Il2CppReflector model;

        public Il2CppDumper(Il2CppInspector proc) {
            model = new Il2CppReflector(proc);
        }

        public void WriteFile(string outFile) {
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create))) {
                foreach (var asm in model.Assemblies) {
                    writer.Write($"// Image {asm.Index}: {asm.FullName} - {asm.Definition.typeStart}\n");
                }

                foreach (var type in model.Assemblies.SelectMany(x => x.DefinedTypes)) {
                    writer.Write($"// Namespace: {type.Namespace}\n");

                    if (type.IsSerializable)
                        writer.Write("[Serializable]\n");
                    if (type.IsPublic)
                        writer.Write("public ");
                    if (type.IsAbstract)
                        writer.Write("abstract ");
                    if (type.IsSealed && !type.IsValueType)
                        writer.Write("sealed ");
                    if (type.IsInterface)
                        writer.Write("interface ");
                    else if (type.IsValueType)
                        writer.Write("struct ");
                    else
                        writer.Write("class ");

                    var @base = type.ImplementedInterfaces.Select(x => x.CSharpName).ToList();
                    if (type.BaseType != null && type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType")
                        @base.Insert(0, type.BaseType.CSharpName);
                    var baseText = @base.Count > 0 ? " : " + string.Join(", ", @base) : string.Empty;

                    writer.Write($"{type.Name}{baseText} // TypeDefIndex: {type.Index}\n{{\n");

                    if (type.DeclaredFields.Count > 0)
                        writer.Write("\t// Fields\n");

                    foreach (var field in type.DeclaredFields) {
                        writer.Write("\t");
                        if (field.IsPrivate)
                            writer.Write("private ");
                        if (field.IsPublic)
                            writer.Write("public ");
                        if (field.IsStatic)
                            writer.Write("static ");
                        if (field.IsInitOnly)
                            writer.Write("readonly ");
                        writer.Write($"{field.FieldType.CSharpName} {field.Name}");
                        if (field.HasDefaultValue)
                            writer.Write($" = {field.DefaultValueString}");
                        writer.Write("; // 0x{0:X}\n", field.Offset);
                    }
                    if (type.DeclaredFields.Count > 0)
                        writer.Write("\n");

                    if (type.DeclaredMethods.Count > 0)
                        writer.Write("\t// Methods\n");

                    foreach (var method in type.DeclaredMethods) {
                        writer.Write("\t");
                        if (method.IsPrivate)
                            writer.Write("private ");
                        if (method.IsPublic)
                            writer.Write("public ");
                        if (method.IsVirtual)
                            writer.Write("virtual ");
                        if (method.IsStatic)
                            writer.Write("static ");

                        writer.Write($"{method.ReturnType.CSharpName} {method.Name}(");

                        bool first = true;
                        foreach (var param in method.DeclaredParameters) {
                            if (!first)
                                writer.Write(", ");
                            first = false;
                            if (param.IsOptional)
                                writer.Write("optional ");
                            if (param.IsOut)
                                writer.Write("out ");
                            writer.Write($"{param.ParameterType.CSharpName} {param.Name}");
                        }
                        writer.Write("); // 0x{0:X}\n",
                            method.VirtualAddress);
                    }
                    writer.Write("}\n");
                }
            }
        }
    }
}
