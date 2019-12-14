/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Il2CppInspector.Reflection
{
    // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.customattributedata?view=netframework-4.8
    public class CustomAttributeData
    {
        // IL2CPP-specific data
        public Il2CppModel Model => AttributeType.Assembly.Model;
        public int Index { get; set; }

        // The type of the attribute
        public TypeInfo AttributeType { get; set; }

        public (ulong Start, ulong End)? VirtualAddress =>
            // The last one will be wrong but there is no way to calculate it
            (Model.Package.CustomAttributeGenerators[Index], Model.Package.CustomAttributeGenerators[Math.Min(Index + 1, Model.Package.CustomAttributeGenerators.Length - 1)]);

        public override string ToString() => "[" + AttributeType.FullName + "]";

        // Get all the custom attributes for a given assembly, type, member or parameter
        private static IEnumerable<CustomAttributeData> getCustomAttributes(Assembly asm, int customAttributeIndex) {
            if (customAttributeIndex < 0)
                yield break;

            var pkg = asm.Model.Package;

            // Attribute type ranges weren't included before v21 (customASttributeGenerators was though)
            if (pkg.Version < 21)
                yield break;

            var range = pkg.AttributeTypeRanges[customAttributeIndex];
            for (var i = range.start; i < range.start + range.count; i++) {
                var typeIndex = pkg.AttributeTypeIndices[i];

                if (asm.Model.AttributesByIndices.TryGetValue(i, out var attribute)) {
                    yield return attribute;
                    continue;
                }

                attribute = new CustomAttributeData { Index = customAttributeIndex, AttributeType = asm.Model.GetTypeFromUsage(typeIndex) };

                asm.Model.AttributesByIndices.TryAdd(i, attribute);
                yield return attribute;
            }
        }

        private static readonly object gcaLock = new object();
        private static IList<CustomAttributeData> getCustomAttributes(Assembly asm, uint token, int customAttributeIndex) {
            // Force the generation of the collection to be thread-safe
            // Convert the result into a list for thread-safe enumeration
            lock (gcaLock) {
                return getCustomAttributes(asm, asm.Model.GetCustomAttributeIndex(asm, token, customAttributeIndex)).ToList();
            }
        }
            
        public static IList<CustomAttributeData> GetCustomAttributes(Assembly asm) => getCustomAttributes(asm, asm.AssemblyDefinition.token, asm.AssemblyDefinition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(EventInfo evt) => getCustomAttributes(evt.Assembly, evt.Definition.token, evt.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(FieldInfo field) => getCustomAttributes(field.Assembly, field.Definition.token, field.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(MethodBase method) => getCustomAttributes(method.Assembly, method.Definition.token, method.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo param) => getCustomAttributes(param.DeclaringMethod.Assembly, param.Definition.token, param.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(PropertyInfo prop) => getCustomAttributes(prop.Assembly, prop.Definition.token, prop.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(TypeInfo type) => getCustomAttributes(type.Assembly, type.Definition.token, type.Definition.customAttributeIndex);
    }
}
