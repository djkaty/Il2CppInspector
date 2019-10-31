/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Il2CppInspector.Reflection {
    public abstract class MemberInfo
    {
        // Assembly that this member is defined in. Only set when MemberType == TypeInfo
        public Assembly Assembly { get; }

        // Custom attributes for this member
        public IEnumerable<CustomAttributeData> CustomAttributes => throw new NotImplementedException();

        // Type that this type is declared in for nested types
        protected int declaringTypeDefinitionIndex { private get; set; } = -1;
        public TypeInfo DeclaringType => declaringTypeDefinitionIndex != -1? Assembly.Model.TypesByIndex[declaringTypeDefinitionIndex] : null;

        // What sort of member this is, eg. method, field etc.
        public abstract MemberTypes MemberType { get; }

        // Name of the member
        public virtual string Name { get; protected set; }

        // TODO: GetCustomAttributes etc.

        // For top-level members in an assembly (ie. non-nested types)
        protected MemberInfo(Assembly asm) => Assembly = asm;

        // For lower level members, eg. fields, properties etc. and nested types
        protected MemberInfo(TypeInfo declaringType = null) {
            if (declaringType != null) {
                Assembly = declaringType.Assembly;
                declaringTypeDefinitionIndex = declaringType.Index;
            }
        }

        public override string ToString() => Name;
    }
}