using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Reflection
{
    public static class Extensions
    {
        // Convert a list of CustomAttributeData objects into C#-friendly attribute usages
        public static string ToString(this IEnumerable<CustomAttributeData> attributes, string linePrefix = "", string attributePrefix = "", bool inline = false, bool emitPointer = false) {
            var sb = new StringBuilder();

            foreach (var cad in attributes) {
                var name = cad.AttributeType.CSharpName;
                var suffix = name.LastIndexOf("Attribute", StringComparison.Ordinal);
                if (suffix != -1)
                    name = name[..suffix];
                sb.Append($"{linePrefix}[{attributePrefix}{name}]");
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

        public static string ToAddressString(this (ulong start, ulong end)? address) => ToAddressString(address?.start ?? 0) + "-" + ToAddressString(address?.end ?? 0);

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

        // Output a value in C#-friendly syntax
        public static string ToCSharpValue(this object value) {
            if (value is bool)
                return (bool) value ? "true" : "false";
            if (value is string str) {
                // Replace standard escape characters
                var s = new StringBuilder();
                for (var i = 0; i < str.Length; i++)
                    // Standard escape characters
                    s.Append(escapeChars.ContainsKey(str[i]) ? escapeChars[str[i]]
                        // Replace everything else with UTF-16 Unicode
                        : str[i] < 32 || str[i] > 126 ? @"\u" + $"{str[i]:X4}"
                        : str[i].ToString());
                return $"\"{s}\"";
            }
            if (!(value is char))
                return (value?.ToString() ?? "null");
            var cValue = (int) (char) value;
            if (cValue < 32 || cValue > 126)
                return $"'\\x{cValue:x4}'";
            return $"'{value}'";
        }
    }
}
