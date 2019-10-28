/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public class PropertyInfo : MemberInfo
    {
        public bool CanRead => GetMethod != null;
        public bool CanWrite => SetMethod != null;

        // TODO: CustomAttributes

        public MethodInfo GetMethod { get; }
        public MethodInfo SetMethod { get; }

        public override string Name { get; protected set; }

        public TypeInfo PropertyType => GetMethod?.ReturnType ?? SetMethod.DeclaredParameters[0].ParameterType;

        public override MemberTypes MemberType => MemberTypes.Property;

        public PropertyInfo(Il2CppInspector pkg, int propIndex, TypeInfo declaringType) :
            base(declaringType) {
            var prop = pkg.Properties[propIndex];

            Name = pkg.Strings[prop.nameIndex];

            // prop.get and prop.set are method indices from the first method of the declaring type
            if (prop.get >= 0)
                GetMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + prop.get);
            if (prop.set >= 0)
                SetMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + prop.set);
        }
    }
}