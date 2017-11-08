/*
    Copyright 2017 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using System;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public class ParameterInfo
    {
        // Information/flags about the parameter
        public ParameterAttributes Attributes { get; }

        // TODO: CustomAttributes

        // True if the parameter has a default value
        public bool HasDefaultValue { get; }

        // Default value for the parameter
        public object DefaultValue { get; }

        public bool IsIn { get; }
        public bool IsOptional { get; }
        public bool IsOut { get; }

        // The member in which the parameter is defined
        public MemberInfo Member { get; }

        // Name of parameter
        public string Name { get; }

        // Type of this parameter
        public TypeInfo ParameterType { get; }

        // Zero-indexed position of the parameter in parameter list
        public int Position { get; }
    }
}
