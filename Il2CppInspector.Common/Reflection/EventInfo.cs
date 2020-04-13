/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class EventInfo : MemberInfo
    {
        // IL2CPP-specific data
        public Il2CppEventDefinition Definition { get; }
        public int Index { get; }
        // Root definition: the event with Definition != null
        protected readonly EventInfo rootDefinition;

        // Information/flags about the event
        public EventAttributes Attributes { get; }

        // Custom attributes for this member
        public override IEnumerable<CustomAttributeData> CustomAttributes => CustomAttributeData.GetCustomAttributes(rootDefinition);

        // Methods for the event
        public MethodInfo AddMethod { get; }
        public MethodInfo RemoveMethod { get; }
        public MethodInfo RaiseMethod { get; }

        // Event handler delegate type
        private readonly TypeRef eventTypeReference;
        public TypeInfo EventHandlerType => eventTypeReference.Value;

        // True if the event has a special name
        public bool IsSpecialName => (Attributes & EventAttributes.SpecialName) == EventAttributes.SpecialName;

        public override MemberTypes MemberType => MemberTypes.Event;

        public EventInfo(Il2CppInspector pkg, int eventIndex, TypeInfo declaringType) :
            base(declaringType) {
            Definition = pkg.Events[eventIndex];
            Index = eventIndex;
            Name = pkg.Strings[Definition.nameIndex];
            rootDefinition = this;

            eventTypeReference = TypeRef.FromReferenceIndex(Assembly.Model, Definition.typeIndex);
            var eventType = pkg.TypeReferences[Definition.typeIndex];

            if ((eventType.attrs & Il2CppConstants.FIELD_ATTRIBUTE_SPECIAL_NAME) == Il2CppConstants.FIELD_ATTRIBUTE_SPECIAL_NAME)
                Attributes |= EventAttributes.SpecialName;

            // NOTE: This relies on methods being added to TypeInfo.DeclaredMethods in the same order they are defined in the Il2Cpp metadata
            // add, remove and raise are method indices from the first method of the declaring type
            if (Definition.add >= 0)
                AddMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + Definition.add);
            if (Definition.remove >= 0)
                RemoveMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + Definition.remove);
            if (Definition.raise >= 0)
                RaiseMethod = declaringType.DeclaredMethods.First(x => x.Index == declaringType.Definition.methodStart + Definition.raise);
        }

        public EventInfo(EventInfo eventDef, TypeInfo declaringType) : base(declaringType) {
            rootDefinition = eventDef;

            Name = eventDef.Name;
            Attributes = eventDef.Attributes;
            eventTypeReference = TypeRef.FromTypeInfo(eventDef.EventHandlerType.SubstituteGenericArguments(declaringType.GetGenericArguments()));

            AddMethod = declaringType.GetMethodByDefinition(eventDef.AddMethod);
            RemoveMethod = declaringType.GetMethodByDefinition(eventDef.RemoveMethod);
            RaiseMethod = declaringType.GetMethodByDefinition(eventDef.RaiseMethod);
        }
    }
}