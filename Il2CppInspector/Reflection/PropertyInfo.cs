/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class PropertyInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppPropertyDefinition Definition { get; }
        public int Index { get; }

        public bool CanRead => GetMethod != null;
        public bool CanWrite => SetMethod != null;

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(this);

        public MethodInfo GetMethod { get; }
        public MethodInfo SetMethod { get; }

        public bool IsAutoProperty => DeclaringType.DeclaredFields.Any(f => f.Name == $"<{Name}>k__BackingField");

        public override string Name { get; protected set; }

        public string CSharpName =>
            // Explicit interface implementation
            Name.IndexOf('.') != -1? string.Join('.', Name.Split('.')[^2..])

            // Regular method
            : Name;

        public TypeInfo PropertyType => GetMethod?.ReturnType ?? SetMethod.DeclaredParameters[^1].ParameterType;

        public override MemberTypes MemberType => MemberTypes.Property;

        public PropertyInfo(Il2CppInspector pkg, int propIndex, TypeInfo declaringType) :
            base(declaringType) {
            Index = propIndex;
            Definition = pkg.Properties[propIndex];
            Name = pkg.Strings[Definition.nameIndex];

            // prop.get and prop.set are method indices from the first method of the declaring type
            if (Definition.get >= 0)
                GetMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + Definition.get);
            if (Definition.set >= 0)
                SetMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + Definition.set);
        }
    }
}