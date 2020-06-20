/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

namespace Il2CppInspector.Reflection
{
    /// <summary>
    /// A class which lazily refers to a TypeInfo instance
    /// </summary>
    internal class TypeRef {
        private Il2CppModel model;
        private int referenceIndex = -1;
        private int definitionIndex = -1;
        private TypeInfo typeInfo = null;

        private TypeRef() { }

        public TypeInfo Value {
            get {
                if (referenceIndex != -1)
                    return model.TypesByReferenceIndex[referenceIndex];
                if (definitionIndex != -1)
                    return model.TypesByDefinitionIndex[definitionIndex];
                return typeInfo;
            }
        }

        public static TypeRef FromReferenceIndex(Il2CppModel model, int index)
            => new TypeRef { model = model, referenceIndex = index };

        public static TypeRef FromDefinitionIndex(Il2CppModel model, int index)
            => new TypeRef { model = model, definitionIndex = index };

        public static TypeRef FromTypeInfo(TypeInfo type)
            => new TypeRef { typeInfo = type };
    }
}
