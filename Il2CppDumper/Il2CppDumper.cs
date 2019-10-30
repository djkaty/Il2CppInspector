﻿// Copyright (c) 2017-2019 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Text;
using Il2CppInspector.Reflection;
using ParameterInfo = Il2CppInspector.Reflection.ParameterInfo;

namespace Il2CppInspector
{
    public class Il2CppDumper
    {
        private readonly Il2CppReflector model;

        public Il2CppDumper(Il2CppInspector proc) {
            model = new Il2CppReflector(proc);
        }

        private string formatAddress(ulong address) => model.Package.BinaryImage.Bits == 32
            ? string.Format($"0x{(uint) address:X8}")
            : string.Format($"0x{address:X16}");

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

                    // Roll-up multicast delegates to use the 'delegate' syntactic sugar
                    if (type.IsClass && type.IsSealed && type.BaseType?.FullName == "System.MulticastDelegate") {
                        var del = type.DeclaredMethods.First(x => x.Name == "Invoke");
                        writer.Write($"delegate {del.ReturnType.CSharpName} {type.Name}(");

                        bool first = true;
                        foreach (var param in del.DeclaredParameters) {
                            if (!first)
                                writer.Write(", ");
                            first = false;
                            if (param.IsOptional)
                                writer.Write("optional ");
                            if (param.IsOut)
                                writer.Write("out ");
                            writer.Write($"{param.ParameterType.CSharpName} {param.Name}");
                        }
                        writer.Write($"); // TypeDefIndex: {type.Index}; {formatAddress(del.VirtualAddress)}\n");
                        continue;
                    }

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
                            writer.Write(";");
                            // Don't output field indices for const fields (they don't have any storage)
                            if (!field.IsLiteral)
                                writer.Write(" // 0x{0:X2}", (uint) field.Offset);
                            writer.WriteLine("");
                        }
                        if (type.DeclaredFields.Count > 0)
                            writer.Write("\n");
                    }

                    // Enumeration
                    else {
                        writer.Write(string.Join(",\n", type.GetEnumNames().Zip(type.GetEnumValues().OfType<object>(),
                            (k, v) => new { k, v }).OrderBy(x => x.v).Select(x => $"\t{x.k} = {x.v}")) + "\n");
                    }

                    var usedMethods = new List<Reflection.MethodInfo>();

                    // Properties
                    if (type.DeclaredProperties.Count > 0)
                        writer.Write("\t// Properties\n");

                    foreach (var prop in type.DeclaredProperties) {
                        string modifiers = prop.GetMethod?.GetModifierString() ?? prop.SetMethod.GetModifierString();
                        writer.Write($"\t{modifiers}{prop.PropertyType.CSharpName} {prop.Name} {{ ");
                        writer.Write((prop.GetMethod != null ? "get; " : "") + (prop.SetMethod != null ? "set; " : "") + "}");
                        if ((prop.GetMethod != null && prop.GetMethod.VirtualAddress != 0) || (prop.SetMethod != null && prop.SetMethod.VirtualAddress != 0))
                            writer.Write(" // ");
                        writer.Write((prop.GetMethod != null && prop.GetMethod.VirtualAddress != 0? formatAddress(prop.GetMethod.VirtualAddress) + " " : "")
                                    + (prop.SetMethod != null && prop.SetMethod.VirtualAddress != 0? formatAddress(prop.SetMethod.VirtualAddress) : "") + "\n");
                        usedMethods.Add(prop.GetMethod);
                        usedMethods.Add(prop.SetMethod);
                    }
                    if (type.DeclaredProperties.Count > 0)
                        writer.Write("\n");

                    // Events
                    if (type.DeclaredEvents.Count > 0)
                        writer.Write("\t// Events\n");

                    foreach (var evt in type.DeclaredEvents) {
                        string modifiers = evt.AddMethod?.GetModifierString();
                        writer.Write($"\t{modifiers}event {evt.EventHandlerType.CSharpName} {evt.Name} {{\n");
                        var m = new Dictionary<string, ulong>();
                        if (evt.AddMethod != null) m.Add("add", evt.AddMethod.VirtualAddress);
                        if (evt.RemoveMethod != null) m.Add("remove", evt.RemoveMethod.VirtualAddress);
                        if (evt.RaiseMethod != null) m.Add("raise", evt.RaiseMethod.VirtualAddress);
                        writer.Write(string.Join("\n", m.Select(x => $"\t\t{x.Key}; // {formatAddress(x.Value)}")) + "\n\t}\n");
                        usedMethods.Add(evt.AddMethod);
                        usedMethods.Add(evt.RemoveMethod);
                        usedMethods.Add(evt.RaiseMethod);
                    }
                    if (type.DeclaredEvents.Count > 0)
                        writer.Write("\n");

                    // Constructors
                    if (type.DeclaredConstructors.Any())
                        writer.Write("\t// Constructors\n");

                    foreach (var method in type.DeclaredConstructors) {
                        writer.Write($"\t{method.GetModifierString()}{method.DeclaringType.Name}(");
                        writer.Write(getParametersString(method.DeclaredParameters));
                        writer.Write(");" + (method.VirtualAddress != 0 ? $" // {formatAddress(method.VirtualAddress)}" : "") + "\n");
                    }
                    if (type.DeclaredConstructors.Any())
                        writer.Write("\n");

                    // Methods
                    if (type.DeclaredMethods.Except(usedMethods).Any())
                        writer.Write("\t// Methods\n");

                    // Don't re-output methods for constructors, properties, events etc.
                    foreach (var method in type.DeclaredMethods.Except(usedMethods)) {
                        writer.Write($"\t{method.GetModifierString()}");
                        if (method.Name != "op_Implicit" && method.Name != "op_Explicit")
                            writer.Write($"{method.ReturnType.CSharpName} {method.CSharpName}");
                        else
                            writer.Write($"{method.CSharpName}{method.ReturnType.CSharpName}");
                        writer.Write("(" + getParametersString(method.DeclaredParameters));
                        writer.Write(");" + (method.VirtualAddress != 0? $" // {formatAddress(method.VirtualAddress)}" : "") + "\n");
                    }
                    writer.Write("}\n");
                }
            }
        }

        public void WriteScript(string scriptFile)
        {
            using (var writer = new StreamWriter(new FileStream(scriptFile, FileMode.Create), Encoding.UTF8))
            {
                // TODO: Copy this template from the resources
                // TODO: 95% of this template is from Il2CppDumper because i'm not familiar with Python
                writer.WriteLine("#encoding: utf-8");
                writer.WriteLine("import idaapi");
                writer.WriteLine("import random");
                writer.WriteLine("def SetName(address, name):");
                writer.WriteLine("	i = 0");
                writer.WriteLine("	returned = idc.MakeNameEx(address, name, SN_NOWARN)");
                writer.WriteLine("	if returned == 0:");
                writer.WriteLine("		new_name = name + '_' + str(addr)");
                writer.WriteLine("		idc.MakeNameEx(address, str(new_name), SN_NOWARN)");
                writer.WriteLine("def SetString(address, comm):");
                writer.WriteLine("	global index");
                writer.WriteLine("	name = \"StringLiteral_\" + str(index);");
                writer.WriteLine("	returned = idc.MakeNameEx(address, name, SN_NOWARN)");
                writer.WriteLine("	idc.MakeComm(address, comm)");
                writer.WriteLine("	index += 1");
                writer.WriteLine("def MakeFunction(start, end):");
                writer.WriteLine("	if GetFunctionAttr(start, FUNCATTR_START) == 0xFFFFFFFF:");
                writer.WriteLine("		idc.MakeFunction(start, end)");
                writer.WriteLine("	else:");
                writer.WriteLine("		idc.SetFunctionEnd(start, end)");
                writer.WriteLine("	index = 1");
            }
        }

        private string getParametersString(List<ParameterInfo> @params) {
            StringBuilder sb = new StringBuilder();

            bool first = true;
            foreach (var param in @params) {
                if (!first)
                    sb.Append(", ");
                first = false;
                if (param.IsOptional)
                    sb.Append("optional ");
                if (param.IsOut)
                    sb.Append("out ");
                sb.Append($"{param.ParameterType.CSharpName} {param.Name}");
            }
            return sb.ToString();
        }
    }
}
