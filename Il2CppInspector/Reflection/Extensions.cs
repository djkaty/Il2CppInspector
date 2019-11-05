using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.Reflection
{
    public static class Extensions
    {
        // Convert a list of CustomAttributeData objects into C#-friendly attribute usages
        public static string ToString(this IEnumerable<CustomAttributeData> attributes, string linePrefix = "", string attributePrefix = "", bool inline = false) {
            var sb = new StringBuilder();

            foreach (var cad in attributes) {
                var name = cad.AttributeType.CSharpName;
                var suffix = name.LastIndexOf("Attribute", StringComparison.Ordinal);
                if (suffix != -1)
                    name = name[..suffix];
                sb.Append($"{linePrefix}[{attributePrefix}{name}] {(inline? "/*" : "//")} {((ulong)cad.VirtualAddress).ToAddressString()}{(inline? " */ " : "\n")}");
            }

            return sb.ToString();
        }

        // Output a ulong as a 32 or 64-bit hexadecimal address
        public static string ToAddressString(this ulong address) => address <= 0xffff_ffff
            ? string.Format($"0x{(uint)address:X8}")
            : string.Format($"0x{address:X16}");

        // Output a value in C#-friendly syntax
        public static string ToCSharpValue(this object value) {
            if (value is bool)
                return (bool) value ? "true" : "false";
            if (value is string)
                return $"\"{value}\"";
            if (!(value is char))
                return (value?.ToString() ?? "null");
            var cValue = (int) (char) value;
            if (cValue < 32 || cValue > 126)
                return $"'\\x{cValue:x4}'";
            return $"'{value}'";
        }
    }
}
