/*
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public abstract class MemberInfo
    {
        // Assembly that this member is defined in. Only set when MemberType == TypeInfo
        public Assembly Assembly { get; protected set; }

        // Custom attributes for this member
        public abstract IEnumerable<CustomAttributeData> CustomAttributes { get; }

        public CustomAttributeData[] GetCustomAttributes(string fullTypeName) => CustomAttributes.Where(a => a.AttributeType.FullName == fullTypeName).ToArray();

        // Type that this type is declared in for nested types
        public virtual TypeInfo DeclaringType { get; private set; }

        // What sort of member this is, eg. method, field etc.
        public abstract MemberTypes MemberType { get; }

        // Name of the member
        public virtual string Name { get; protected set; }

        // Name of the member with @ prepended if the name is a C# reserved keyword
        public string CSharpSafeName => Constants.Keywords.Contains(Name) ? "@" + Name : Name;

        // For top-level members in an assembly (ie. non-nested types)
        protected MemberInfo(Assembly asm) => Assembly = asm;

        // For lower level members, eg. fields, properties etc. and nested types
        protected MemberInfo(TypeInfo declaringType) {
            Assembly = declaringType.Assembly;
            DeclaringType = declaringType;
        }

        public override string ToString() => Name;
    }
}