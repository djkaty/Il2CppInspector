// Copyright (c) 2017-2019 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Il2CppInspector.Reflection;
using MethodInfo = Il2CppInspector.Reflection.MethodInfo;
using TypeInfo = Il2CppInspector.Reflection.TypeInfo;

namespace Il2CppInspector
{
    public class Il2CppCSharpDumper
    {
        private readonly Il2CppModel model;

        // Namespace prefixes whose contents should be skipped
        public List<string> ExcludedNamespaces { get; set; }

        // Suppress types, fields and methods with the CompilerGenerated attribute; suppress the attribute itself from property getters and setters
        public bool SuppressGenerated { get; set; }

        // Suppress binary metadata in code comments
        public bool SuppressMetadata { get; set; }

        private const string CGAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
        private const string FBAttribute = "System.Runtime.CompilerServices.FixedBufferAttribute";
        private const string ExtAttribute = "System.Runtime.CompilerServices.ExtensionAttribute";
        private const string DMAttribute = "System.Reflection.DefaultMemberAttribute";

        public Il2CppCSharpDumper(Il2CppModel model) => this.model = model;

        public void WriteSingleFile(string outFile) => WriteSingleFile(outFile, t => t.Index);

        public void WriteSingleFile<TKey>(string outFile, Func<TypeInfo, TKey> orderBy) => writeFile(outFile, model.Assemblies.SelectMany(x => x.DefinedTypes).OrderBy(orderBy));

        public void WriteFilesByNamespace<TKey>(string outPath, Func<TypeInfo, TKey> orderBy, bool flattenHierarchy) {
            var namespaces = model.Assemblies.SelectMany(x => x.DefinedTypes).GroupBy(t => t.Namespace);
            foreach (var ns in namespaces)
                writeFile($"{outPath}\\{(!string.IsNullOrEmpty(ns.Key)? ns.Key : "global").Replace('.', flattenHierarchy? '.' : '\\')}.cs", ns.OrderBy(orderBy));
        }

        public void WriteFilesByAssembly<TKey>(string outPath, Func<TypeInfo, TKey> orderBy) {
            foreach (var asm in model.Assemblies) {
                // Sort namespaces into alphabetical order, then sort types within the namespaces by the specified sort function
                writeFile($"{outPath}\\{asm.FullName}.cs", asm.DefinedTypes.OrderBy(t => t.Namespace).ThenBy(orderBy));
            }
        }

        public void WriteFilesByClass(string outPath, bool flattenHierarchy) {
            foreach (var type in model.Assemblies.SelectMany(x => x.DefinedTypes))
                writeFile($"{outPath}\\{type.FullName.Replace('.', flattenHierarchy ? '.' : '\\')}.cs", new [] {type});
        }

        private void writeFile(string outFile, IEnumerable<TypeInfo> types, bool useNamespaceSyntax = true) {

            var nsRefs = new HashSet<string>();
            var code = new StringBuilder();
            var nsContext = "";
            var usedTypes = new List<TypeInfo>();

            foreach (var type in types) {

                // Skip namespace and any children if requested
                if (ExcludedNamespaces?.Any(x => x == type.Namespace || type.Namespace.StartsWith(x + ".")) ?? false)
                    continue;

                // Assembly.DefinedTypes returns nested types in the assembly by design - ignore them
                if (type.IsNested)
                    continue;

                // Get code
                var text = generateType(type);
                if (string.IsNullOrEmpty(text))
                    continue;

                // Determine if we need to change namespace (after we have established the code block is not empty)
                if (useNamespaceSyntax) {
                    if (type.Namespace != nsContext) {
                        if (!string.IsNullOrEmpty(nsContext))
                            code.Remove(code.Length - 1, 1).Append("}\n\n");

                        if (!string.IsNullOrEmpty(type.Namespace))
                            code.Append("namespace " + type.Namespace + "\n{\n");

                        nsContext = type.Namespace;
                    }

                    if (!string.IsNullOrEmpty(nsContext)) {
                        text = "\t" + text.Replace("\n", "\n\t");
                        text = text.Remove(text.Length - 1);
                    }
                }

                // Append namespace
                if (!useNamespaceSyntax)
                    code.Append($"// Namespace: {(!string.IsNullOrEmpty(type.Namespace) ? type.Namespace : "<global namespace>")}\n");
                
                // Determine namespace references
                var refs = type.GetAllTypeReferences();
                var ns = refs.Where(r => !string.IsNullOrEmpty(r.Namespace) && r.Namespace != type.Namespace).Select(r => r.Namespace);
                nsRefs.UnionWith(ns);

                // Append type definition
                code.Append(text + "\n");

                // Add to list of used types
                usedTypes.Add(type);
            }

            // Stop if nothing to output
            if (!usedTypes.Any())
                return;

            // Close namespace
            if (useNamespaceSyntax && !string.IsNullOrEmpty(nsContext))
                code.Remove(code.Length - 1, 1).Append("}\n");
            
            // Determine assemblies used in this file
            var assemblies = types.Select(t => t.Assembly).Distinct();

            // Add assembly attribute namespaces to reference list
            nsRefs.UnionWith(assemblies.SelectMany(a => a.CustomAttributes).Select(a => a.AttributeType.Namespace));

            // Determine using directives (put System namespaces first)
            var usings = nsRefs.OrderBy(n => (n.StartsWith("System.") || n == "System") ? "0" + n : "1" + n);

            // Ensure output directory exists
            if (!string.IsNullOrEmpty(Path.GetDirectoryName(outFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));

            // Create output file
            using StreamWriter writer = new StreamWriter(new FileStream(outFile, FileMode.Create), Encoding.UTF8);

            // Write preamble
            writer.Write(@"/*
 * Generated code file by Il2CppInspector - http://www.djkaty.com - https://github.com/djkaty
 */

");

            // Output using directives
            writer.Write(string.Concat(usings.Select(n => $"using {n};\n")));
            if (nsRefs.Any())
                writer.Write("\n");

            // Output assembly information and attributes
            writer.Write(generateAssemblyInfo(assemblies) + "\n\n");

            // Output type definitions
            writer.Write(code);
        }

        private string generateAssemblyInfo(IEnumerable<Reflection.Assembly> assemblies) {
            var text = new StringBuilder();

            foreach (var asm in assemblies) {
                text.Append($"// Image {asm.Index}: {asm.FullName} - {asm.ImageDefinition.typeStart}\n");

                // Assembly-level attributes
                text.Append(asm.CustomAttributes.Where(a => a.AttributeType.FullName != ExtAttribute).OrderBy(a => a.AttributeType.Name).ToString(attributePrefix: "assembly: ", emitPointer: !SuppressMetadata));
                if (asm.CustomAttributes.Any())
                    text.Append("\n");
            }
            return text.ToString().TrimEnd();
        }

        private string generateType(TypeInfo type, string prefix = "") {
            // Don't output compiler-generated types if desired
            if (SuppressGenerated && type.GetCustomAttributes(CGAttribute).Any())
                return string.Empty;

            var codeBlocks = new Dictionary<string, string>();
            var usedMethods = new List<MethodInfo>();
            var sb = new StringBuilder();

            // Fields
            sb.Clear();
            if (!type.IsEnum) {
                foreach (var field in type.DeclaredFields) {
                    if (SuppressGenerated && field.GetCustomAttributes(CGAttribute).Any())
                        continue;

                    if (field.IsNotSerialized)
                        sb.Append(prefix + "\t[NonSerialized]\n");

                    // Attributes
                    sb.Append(field.CustomAttributes.Where(a => a.AttributeType.FullName != FBAttribute).OrderBy(a => a.AttributeType.Name).ToString(prefix + "\t", emitPointer: !SuppressMetadata));
                    sb.Append(prefix + "\t");
                    sb.Append(field.GetModifierString());

                    // Fixed buffers
                    if (field.GetCustomAttributes(FBAttribute).Any()) {
                        if (!SuppressMetadata)
                            sb.Append($"/* {((ulong) field.GetCustomAttributes(FBAttribute)[0].VirtualAddress).ToAddressString()} */ ");
                        sb.Append($"{field.FieldType.DeclaredFields[0].FieldType.CSharpName} {field.Name}[0]"); // FixedElementField
                    }
                    // Regular fields
                    else
                        sb.Append($"{field.FieldType.CSharpName} {field.Name}");
                    if (field.HasDefaultValue)
                        sb.Append($" = {field.DefaultValueString}");
                    sb.Append(";");
                    // Don't output field indices for const fields (they don't have any storage)
                    if (!field.IsLiteral && !SuppressMetadata)
                        sb.Append($" // 0x{(uint) field.Offset:X2}");
                    sb.Append("\n");
                }
                codeBlocks.Add("Fields", sb.ToString());
            }

            // Properties
            sb.Clear();
            foreach (var prop in type.DeclaredProperties) {
                // Attributes
                sb.Append(prop.CustomAttributes.OrderBy(a => a.AttributeType.Name).ToString(prefix + "\t", emitPointer: !SuppressMetadata));

                // The access mask enum values go from 1 (private) to 6 (public) in order from most to least restrictive
                var getAccess = (prop.GetMethod?.Attributes ?? 0) & MethodAttributes.MemberAccessMask;
                var setAccess = (prop.SetMethod?.Attributes ?? 0) & MethodAttributes.MemberAccessMask;

                var primary = getAccess >= setAccess ? prop.GetMethod : prop.SetMethod;
                sb.Append($"{prefix}\t{primary.GetModifierString()}{prop.PropertyType.CSharpName} ");

                // Non-indexer
                if ((!prop.CanRead || !prop.GetMethod.DeclaredParameters.Any()) && (!prop.CanWrite || prop.SetMethod.DeclaredParameters.Count == 1))
                    sb.Append($"{prop.Name} {{ ");
                // Indexer
                else
                    sb.Append("this[" + string.Join(", ", primary.DeclaredParameters.SkipLast(getAccess >= setAccess? 0 : 1).Select(p => p.GetParameterString(!SuppressMetadata))) + "] { ");

                sb.Append((prop.CanRead? prop.GetMethod.CustomAttributes.Where(a => !SuppressGenerated || a.AttributeType.FullName != CGAttribute).ToString(inline: true, emitPointer: !SuppressMetadata) 
                                               + (getAccess < setAccess? prop.GetMethod.GetAccessModifierString() : "") + "get; " : "")
                             + (prop.CanWrite? prop.SetMethod.CustomAttributes.Where(a => !SuppressGenerated || a.AttributeType.FullName != CGAttribute).ToString(inline: true, emitPointer: !SuppressMetadata) 
                                               + (setAccess < getAccess? prop.SetMethod.GetAccessModifierString() : "") + "set; " : "") + "}");
                if (!SuppressMetadata) {
                    if ((prop.CanRead && prop.GetMethod.VirtualAddress != 0) || (prop.CanWrite && prop.SetMethod.VirtualAddress != 0))
                        sb.Append(" // ");
                    sb.Append((prop.CanRead && prop.GetMethod.VirtualAddress != 0 ? prop.GetMethod.VirtualAddress.ToAddressString() + " " : "")
                                + (prop.CanWrite && prop.SetMethod.VirtualAddress != 0 ? prop.SetMethod.VirtualAddress.ToAddressString() : ""));
                }
                sb.Append("\n");

                usedMethods.Add(prop.GetMethod);
                usedMethods.Add(prop.SetMethod);
            }
            codeBlocks.Add("Properties", sb.ToString());

            // Events
            sb.Clear();
            foreach (var evt in type.DeclaredEvents) {
                // Attributes
                sb.Append(evt.CustomAttributes.OrderBy(a => a.AttributeType.Name).ToString(prefix + "\t", emitPointer: !SuppressMetadata));

                string modifiers = evt.AddMethod?.GetModifierString();
                sb.Append($"{prefix}\t{modifiers}event {evt.EventHandlerType.CSharpName} {evt.Name} {{\n");
                var m = new Dictionary<string, ulong>();
                if (evt.AddMethod != null) m.Add("add", evt.AddMethod.VirtualAddress);
                if (evt.RemoveMethod != null) m.Add("remove", evt.RemoveMethod.VirtualAddress);
                if (evt.RaiseMethod != null) m.Add("raise", evt.RaiseMethod.VirtualAddress);
                sb.Append(string.Join("\n", m.Select(x => $"{prefix}\t\t{x.Key};{(SuppressMetadata? "" : " // " + x.Value.ToAddressString())}")) + "\n" + prefix + "\t}\n");
                usedMethods.Add(evt.AddMethod);
                usedMethods.Add(evt.RemoveMethod);
                usedMethods.Add(evt.RaiseMethod);
            }
            codeBlocks.Add("Events", sb.ToString());

            // Nested types
            codeBlocks.Add("Nested types", string.Join("\n", type.DeclaredNestedTypes.Select(n => generateType(n, prefix + "\t")).Where(c => !string.IsNullOrEmpty(c))));

            // Constructors
            sb.Clear();
            foreach (var method in type.DeclaredConstructors) {
                // Attributes
                sb.Append(method.CustomAttributes.OrderBy(a => a.AttributeType.Name).ToString(prefix + "\t", emitPointer: !SuppressMetadata));

                sb.Append($"{prefix}\t{method.GetModifierString()}{method.DeclaringType.UnmangledBaseName}{method.GetTypeParametersString()}(");
                sb.Append(method.GetParametersString(!SuppressMetadata) + ");");
                sb.Append((!SuppressMetadata && method.VirtualAddress != 0 ? $" // {method.VirtualAddress.ToAddressString()}" : "") + "\n");
            }
            codeBlocks.Add("Constructors", sb.ToString());

            // Methods
            // Don't re-output methods for constructors, properties, events etc.
            var methods = type.DeclaredMethods.Except(usedMethods).Where(m => m.CustomAttributes.All(a => a.AttributeType.FullName != ExtAttribute));
            codeBlocks.Add("Methods", string.Concat(methods.Select(m => generateMethod(m, prefix))));
            usedMethods.AddRange(methods);

            // Extension methods 
            codeBlocks.Add("Extension methods", string.Concat(type.DeclaredMethods.Except(usedMethods).Select(m => generateMethod(m, prefix))));

            // Type declaration
            sb.Clear();

            if (type.IsImport)
                sb.Append(prefix + "[ComImport]\n");
            if (type.IsSerializable)
                sb.Append(prefix + "[Serializable]\n");

            // TODO: DefaultMemberAttribute should be output if it is present and the type does not have an indexer, otherwise suppressed
            // See https://docs.microsoft.com/en-us/dotnet/api/system.reflection.defaultmemberattribute?view=netframework-4.8
            sb.Append(type.CustomAttributes.Where(a => a.AttributeType.FullName != DMAttribute && a.AttributeType.FullName != ExtAttribute)
                                            .OrderBy(a => a.AttributeType.Name).ToString(prefix, emitPointer: !SuppressMetadata));

            // Roll-up multicast delegates to use the 'delegate' syntactic sugar
            if (type.IsClass && type.IsSealed && type.BaseType?.FullName == "System.MulticastDelegate") {
                sb.Append(prefix + type.GetAccessModifierString());

                var del = type.GetMethod("Invoke");
                // IL2CPP doesn't seem to retain return type attributes
                //sb.Append(del.ReturnType.CustomAttributes.ToString(prefix, "return: ", emitPointer: !SuppressMetadata));
                if (del.RequiresUnsafeContext)
                    sb.Append("unsafe ");
                sb.Append($"delegate {del.ReturnType.CSharpName} {type.CSharpTypeDeclarationName}(");
                sb.Append(del.GetParametersString(!SuppressMetadata) + ");");
                if (!SuppressMetadata)
                    sb.Append($" // TypeDefIndex: {type.Index}; {del.VirtualAddress.ToAddressString()}");
                sb.Append("\n");
                return sb.ToString();
            }

            sb.Append(prefix + type.GetModifierString());

            var @base = type.ImplementedInterfaces.Select(x => x.CSharpName).ToList();
            if (type.BaseType != null && type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType" && !type.IsEnum)
                @base.Insert(0, type.BaseType.CSharpName);
            if (type.IsEnum && type.GetEnumUnderlyingType().FullName != "System.Int32") // enums derive from int by default
                @base.Insert(0, type.GetEnumUnderlyingType().CSharpName);
            var baseText = @base.Count > 0 ? " : " + string.Join(", ", @base) : string.Empty;

            sb.Append($"{type.CSharpTypeDeclarationName}{baseText}");
            if (!SuppressMetadata)
                sb.Append($" // TypeDefIndex: {type.Index}");
            sb.Append("\n");

            if (type.GenericTypeParameters != null)
                foreach (var gp in type.GenericTypeParameters) {
                    var constraint = gp.GetTypeConstraintsString();
                    if (constraint != string.Empty)
                        sb.Append($"{prefix}\t{constraint}\n");
                }

            sb.Append(prefix + "{\n");

            // Enumeration
            if (type.IsEnum) {
                sb.Append(string.Join(",\n", type.GetEnumNames().Zip(type.GetEnumValues().OfType<object>(),
                              (k, v) => new { k, v }).OrderBy(x => x.v).Select(x => $"{prefix}\t{x.k} = {x.v}")) + "\n");
            }

            // Type definition
            else
                sb.Append(string.Join("\n", codeBlocks.Where(b => b.Value != string.Empty).Select(b => prefix + "\t// " + b.Key + "\n" + b.Value)));

            sb.Append(prefix + "}\n");
            return sb.ToString();
        }

        private string generateMethod(MethodInfo method, string prefix) {
            if (SuppressGenerated && method.GetCustomAttributes(CGAttribute).Any())
                return string.Empty;

            var writer = new StringBuilder();

            // Attributes
            writer.Append(method.CustomAttributes.Where(a => a.AttributeType.FullName != ExtAttribute).OrderBy(a => a.AttributeType.Name).ToString(prefix + "\t", emitPointer: !SuppressMetadata));

            // IL2CPP doesn't seem to retain return type attributes
            //writer.Append(method.ReturnType.CustomAttributes.ToString(prefix + "\t", "return: ", emitPointer: !SuppressMetadata));
            writer.Append($"{prefix}\t{method.GetModifierString()}");
            if (method.Name != "op_Implicit" && method.Name != "op_Explicit")
                writer.Append($"{method.ReturnParameter.GetReturnParameterString()} {method.CSharpName}{method.GetTypeParametersString()}");
            else
                writer.Append($"{method.CSharpName}{method.ReturnType.CSharpName}");
            writer.Append("(" + method.GetParametersString(!SuppressMetadata) + ")");

            if (method.GenericTypeParameters != null)
                foreach (var gp in method.GenericTypeParameters) {
                    var constraint = gp.GetTypeConstraintsString();
                    if (constraint != string.Empty)
                        writer.Append($"\n{prefix}\t\t{constraint}");
                }

            writer.Append(";" + (!SuppressMetadata && method.VirtualAddress != 0 ? $" // {method.VirtualAddress.ToAddressString()}" : "") + "\n");

            return writer.ToString();
        }
    }
}
