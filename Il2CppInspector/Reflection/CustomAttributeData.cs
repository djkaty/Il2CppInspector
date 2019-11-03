/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;

namespace Il2CppInspector.Reflection
{
    // See: https://docs.microsoft.com/en-us/dotnet/api/system.reflection.customattributedata?view=netframework-4.8
    public class CustomAttributeData
    {
        // The type of the attribute
        public TypeInfo AttributeType { get; set; }

        // TODO Constructor, ConstructorArguments, NamedArguments

        public override string ToString() => "[" + AttributeType.FullName + "]";

        // Get all the custom attributes for a given assembly, type, member or parameter
        private static IEnumerable<CustomAttributeData> getCustomAttributes(Assembly asm, int customAttributeIndex) {
            var pkg = asm.Model.Package;

            // Attribute type ranges weren't included before v21 (customASttributeGenerators was though)
            if (pkg.Version < 21)
                yield break;

            var range = pkg.AttributeTypeRanges[customAttributeIndex];
            for (var i = range.start; i < range.start + range.count; i++) {
                var typeIndex = pkg.AttributeTypeIndices[i];
                yield return new CustomAttributeData { AttributeType = asm.Model.GetTypeFromUsage(typeIndex) };
            }
        }

        private static IList<CustomAttributeData> getCustomAttributes(Assembly asm, uint token, int customAttributeIndex)
            => (IList<CustomAttributeData>) getCustomAttributes(asm, asm.Model.GetCustomAttributeIndex(asm, token, customAttributeIndex));

        public static IList<CustomAttributeData> GetCustomAttributes(Assembly asm) => getCustomAttributes(asm, asm.Definition.token, -1);
        public static IList<CustomAttributeData> GetCustomAttributes(EventInfo evt) => getCustomAttributes(evt.Assembly, evt.Definition.token, evt.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(FieldInfo field) => getCustomAttributes(field.Assembly, field.Definition.token, field.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(MethodBase method) => getCustomAttributes(method.Assembly, method.Definition.token, method.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo param) => getCustomAttributes(param.Member.Assembly, param.Definition.token, param.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(PropertyInfo prop) => getCustomAttributes(prop.Assembly, prop.Definition.token, prop.Definition.customAttributeIndex);
        public static IList<CustomAttributeData> GetCustomAttributes(TypeInfo type) => getCustomAttributes(type.Assembly, type.Definition.token, type.Definition.customAttributeIndex);
    }
}
