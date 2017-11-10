/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class PropertyInfo : MemberInfo
    {
        public bool CanRead => GetMethod != null;
        public bool CanWrite => SetMethod != null;

        // TODO: CustomAttributes

        public MethodInfo GetMethod { get; }
        public MethodInfo SetMethod { get; }

        public string Name { get; }

        public TypeInfo PropertyType => GetMethod?.ReturnType ?? SetMethod.DeclaredParameters[0].ParameterType;

        public override MemberTypes MemberType => MemberTypes.Property;

        public PropertyInfo(Il2CppInspector pkg, int propIndex, TypeInfo declaringType) :
            base(declaringType) {
            var prop = pkg.Metadata.Properties[propIndex];

            Name = pkg.Strings[prop.nameIndex];

            // NOTE: This relies on methods being added to TypeInfo.DeclaredMethods in the same order they are defined in the Il2Cpp metadata
            // prop.get and prop.set are method indices from the first method of the declaring type
            if (prop.get >= 0)
                GetMethod = declaringType.DeclaredMethods[prop.get];
            if (prop.set >= 0)
                SetMethod = declaringType.DeclaredMethods[prop.set];
        }
    }
}