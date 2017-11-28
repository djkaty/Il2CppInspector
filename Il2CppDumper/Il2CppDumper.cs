// Copyright (c) 2017 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
            using (var writer = new StreamWriter(new FileStream(outFile, FileMode.Create), Encoding.UTF8)) {
                foreach (var asm in model.Assemblies) {
                    writer.Write($"// Image {asm.Index}: {asm.FullName} - {asm.Definition.typeStart}\n");
                }

                foreach (var type in model.Assemblies.SelectMany(x => x.DefinedTypes)) {

                    // Type declaration
                    writer.Write($"\n// Namespace: {type.Namespace}\n");

                    if (type.IsImport)
                        writer.Write("[ComImport]");
                    if (type.IsSerializable)
                        writer.Write("[Serializable]\n");
                    if (type.IsPublic || type.IsNestedPublic)
                        writer.Write("public ");
                    if (type.IsNestedPrivate)
                        writer.Write("private ");
                    if (type.IsNestedFamily)
                        writer.Write("protected ");
                    if (type.IsNestedAssembly || type.IsNotPublic)
                        writer.Write("internal ");
                    if (type.IsNestedFamORAssem)
                        writer.Write("protected internal ");
                    if (type.IsNestedFamANDAssem)
                        writer.Write("[family and assembly] ");
                    // An abstract sealed class is a static class
                    if (type.IsAbstract && type.IsSealed)
                        writer.Write("static ");
                    else {
                        if (type.IsAbstract && !type.IsInterface)
                            writer.Write("abstract ");
                        if (type.IsSealed && !type.IsValueType && !type.IsEnum)
                            writer.Write("sealed ");
                    }
                    if (type.IsInterface)
                        writer.Write("interface ");
                    else if (type.IsValueType)
                        writer.Write("struct ");
                    else if (type.IsEnum)
                        writer.Write("enum ");
                    else
                        writer.Write("class ");

                    var @base = type.ImplementedInterfaces.Select(x => x.CSharpName).ToList();
                    if (type.BaseType != null && type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType" && !type.IsEnum)
                        @base.Insert(0, type.BaseType.CSharpName);
                    if (type.IsEnum && type.ElementType.CSharpName != "int") // enums derive from int by default
                        @base.Insert(0, type.ElementType.CSharpName);
                    var baseText = @base.Count > 0 ? " : " + string.Join(", ", @base) : string.Empty;

                    writer.Write($"{type.Name}{baseText} // TypeDefIndex: {type.Index}\n{{\n");

                    // Fields
                    if (!type.IsEnum) {
                        if (type.DeclaredFields.Count > 0)
                            writer.Write("\t// Fields\n");

                        foreach (var field in type.DeclaredFields) {
                            writer.Write("\t");
                            if (field.IsNotSerialized)
                                writer.Write("[NonSerialized]\t");

                            if (field.IsPrivate)
                                writer.Write("private ");
                            if (field.IsPublic)
                                writer.Write("public ");
                            if (field.IsFamily)
                                writer.Write("protected ");
                            if (field.IsAssembly)
                                writer.Write("internal ");
                            if (field.IsFamilyOrAssembly)
                                writer.Write("protected internal ");
                            if (field.IsFamilyAndAssembly)
                                writer.Write("[family and assembly] ");
                            if (field.IsLiteral)
                                writer.Write("const ");
                            // All const fields are also static by implication
                            else if (field.IsStatic)
                                writer.Write("static ");
                            if (field.IsInitOnly)
                                writer.Write("readonly ");
                            if (field.IsPinvokeImpl)
                                writer.Write("extern ");
                            writer.Write($"{field.FieldType.CSharpName} {field.Name}");
                            if (field.HasDefaultValue)
                                writer.Write($" = {field.DefaultValueString}");
                            writer.Write("; // 0x{0:X}\n", field.Offset);
                        }
                        if (type.DeclaredFields.Count > 0)
                            writer.Write("\n");
                    }
                    else {
                        writer.Write(string.Join(",\n", type.GetEnumNames().Zip(type.GetEnumValues().OfType<object>(),
                            (k, v) => new { k, v }).OrderBy(x => x.v).Select(x => $"\t{x.k} = {x.v}")) + "\n");
                    }

                    // Properties
                    if (type.DeclaredProperties.Count > 0)
                        writer.Write("\t// Properties\n");

                    foreach (var prop in type.DeclaredProperties) {
                        string modifiers = prop.GetMethod?.GetModifierString() ?? prop.SetMethod.GetModifierString();
                        writer.Write($"\t{modifiers} {prop.PropertyType.CSharpName} {prop.Name} {{ ");
                        writer.Write((prop.GetMethod != null ? "get; " : "") + (prop.SetMethod != null ? "set; " : ""));
                        writer.Write("}\n");
                    }
                    if (type.DeclaredProperties.Count > 0)
                        writer.Write("\n");

                    // Events
                    if (type.DeclaredEvents.Count > 0)
                        writer.Write("\t// Events\n");

                    foreach (var evt in type.DeclaredEvents) {
                        string modifiers = evt.AddMethod?.GetModifierString();
                        writer.Write($"\t{modifiers} event {evt.EventHandlerType.CSharpName} {evt.Name} {{\n");
                        var m = new Dictionary<string, uint>();
                        if (evt.AddMethod != null) m.Add("add", evt.AddMethod.VirtualAddress);
                        if (evt.RemoveMethod != null) m.Add("remove", evt.RemoveMethod.VirtualAddress);
                        if (evt.RaiseMethod != null) m.Add("raise", evt.RaiseMethod.VirtualAddress);
                        writer.Write(string.Join("\n", m.Select(x => $"\t\t{x.Key}; // 0x{x.Value:X8}")) + "\n\t}\n");
                    }

                    if (type.DeclaredEvents.Count > 0)
                        writer.Write("\n");

                    // Methods
                    if (type.DeclaredMethods.Count > 0)
                        writer.Write("\t// Methods\n");

                    foreach (var method in type.DeclaredMethods) {
                        writer.Write($"\t{method.GetModifierString()} {method.ReturnType.CSharpName} {method.Name}(");

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
                        writer.Write("); // 0x{0:X8}\n",
                            method.VirtualAddress);
                    }
                    writer.Write("}\n");
                }
            }
        }
    }
}
