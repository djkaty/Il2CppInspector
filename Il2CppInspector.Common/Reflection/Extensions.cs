/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Reflection
{
    public static class Extensions
    {
        // Convert a list of CustomAttributeData objects into C#-friendly attribute usages
        public static string ToString(this IEnumerable<CustomAttributeData> attributes, Scope scope = null,
            string linePrefix = "", string attributePrefix = "", bool inline = false, bool emitPointer = false, bool mustCompile = false) {
            var sb = new StringBuilder();

            foreach (var cad in attributes) {
                // Find a constructor that either has no parameters, or all optional parameters
                var parameterlessConstructor = cad.AttributeType.DeclaredConstructors.Any(c => !c.IsStatic && c.IsPublic && c.DeclaredParameters.All(p => p.IsOptional));

                // IL2CPP doesn't retain attribute arguments so we have to comment out those with non-optional arguments if we want the output to compile
                var commentStart = mustCompile && !parameterlessConstructor? inline? "/* " : "// " : "";
                var commentEnd = commentStart.Length > 0 && inline? " */" : "";
                var arguments = "";

                // Set AttributeUsage(AttributeTargets.All) if making output that compiles to mitigate CS0592
                if (mustCompile && cad.AttributeType.FullName == "System.AttributeUsageAttribute") {
                    commentStart = "";
                    commentEnd = "";
                    arguments = "(AttributeTargets.All)";
                }

                var name = cad.AttributeType.GetScopedCSharpName(scope);
                var suffix = name.LastIndexOf("Attribute", StringComparison.Ordinal);
                if (suffix != -1)
                    name = name[..suffix];
                sb.Append($"{linePrefix}{commentStart}[{attributePrefix}{name}{arguments}]{commentEnd}");
                if (emitPointer)
                    sb.Append($" {(inline? "/*" : "//")} {cad.VirtualAddress.ToAddressString()}{(inline? " */" : "")}");
                sb.Append(inline? " ":"\n");
            }

            return sb.ToString();
        }

        // Output a ulong as a 32 or 64-bit hexadecimal address
        public static string ToAddressString(this ulong address) => address <= 0xffff_ffff
            ? string.Format($"0x{(uint)address:X8}")
            : string.Format($"0x{address:X16}");

        public static string ToAddressString(this long address) => ((ulong) address).ToAddressString();

        public static string ToAddressString(this (ulong start, ulong end)? address) => ToAddressString(address?.start ?? 0) + "-" + ToAddressString(address?.end ?? 0);

        public static string ToAddressString(this (ulong start, ulong end) address) => ToAddressString(address.start) + "-" + ToAddressString(address.end);

        // C# string literal escape characters
        // Taken from: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/strings/#regular-and-verbatim-string-literals
        private static Dictionary<char, string> escapeChars = new Dictionary<char, string> {
            ['\''] = @"\'",
            ['"'] = @"\""",
            ['\\'] = @"\\",
            ['\0'] = @"\0",
            ['\a'] = @"\a",
            ['\b'] = @"\b",
            ['\f'] = @"\f",
            ['\n'] = @"\n",
            ['\r'] = @"\r",
            ['\t'] = @"\t",
            ['\v'] = @"\v"
        };

        // Output a string in Python-friendly syntax
        public static string ToEscapedString(this string str) {
            // Replace standard escape characters
            var s = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
                // Standard escape characters
                s.Append(escapeChars.ContainsKey(str[i]) ? escapeChars[str[i]]
                        // Replace everything else with UTF-16 Unicode
                    : str[i] < 32 || str[i] > 126 ? @"\u" + $"{(int) str[i]:X4}"
                    : str[i].ToString());
            return s.ToString();
        }

        public static string ToCIdentifier(this string str) {
            // replace * with Ptr
            str = str.Replace("*", "Ptr");
            // replace illegal characters
            str = Regex.Replace(str, "[^a-zA-Z0-9_]", "_");
            // ensure identifier starts with a letter or _ (and is non-empty)
            if (!Regex.IsMatch(str, "^[a-zA-Z_]"))
                str = "_" + str;
            return str;
        }

        // Output a value in C#-friendly syntax
        public static string ToCSharpValue(this object value, TypeInfo type, Scope usingScope = null) {
            if (value is bool)
                return (bool) value ? "true" : "false";
            if (value is float)
                return value switch {
                    float.PositiveInfinity => "1F / 0F",
                    float.NegativeInfinity => "-1F / 0F",
                    float.NaN => "0F / 0F",
                    _ => value + "f"
                };
            if (value is double)
                return value switch {
                    double.PositiveInfinity => "1D / 0D",
                    double.NegativeInfinity => "-1D / 0D",
                    double.NaN => "0D / 0D",
                    _ => value.ToString()
                };
            if (value is string str) {
                return $"\"{str.ToEscapedString()}\"";
            }
            if (value is char) {
                var cValue = (int) (char) value;
                if (cValue < 32 || cValue > 126)
                    return $"'\\x{cValue:x4}'";
                return $"'{value}'";
            }
            if (type.IsEnum) {
                var flags = type.GetCustomAttributes("System.FlagsAttribute").Any();
                var values = type.GetEnumNames().Zip(type.GetEnumValues().OfType<object>(), (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
                var typeName = type.GetScopedCSharpName(usingScope);

                // We don't know what type the enumeration or value is, so we use Object.Equals() to do content-based equality testing
                if (!flags) {
                    // Defined enum name
                    if (values.FirstOrDefault(v => v.Value.Equals(value)).Key is string enumValue)
                        return typeName + "." + enumValue;

                    // Undefined enum value (return a cast)
                    return "(" + typeName + ") " + value;
                }

                // Logical OR a series of flags together
                var flagValue = Convert.ToInt64(value);
                var setFlags = values.Where(x => (Convert.ToInt64(x.Value) & flagValue) == Convert.ToInt64(x.Value)).Select(x => typeName + "." + x.Key);
                return string.Join(" | ", setFlags);
            }
            // Structs and generic type parameters must use 'default' rather than 'null'
            return value?.ToString() ?? (type.IsValueType || type.IsGenericParameter? "default" : "null");
        }
    }
}
