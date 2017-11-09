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
        public bool HasDefaultValue => (Attributes & ParameterAttributes.HasDefault) != 0;

        // Default value for the parameter
        public object DefaultValue => throw new NotImplementedException();

        public bool IsIn => (Attributes & ParameterAttributes.In) != 0;
        public bool IsOptional => (Attributes & ParameterAttributes.Optional) != 0;
        public bool IsOut => (Attributes & ParameterAttributes.Out) != 0;
        public bool IsRetval => (Attributes & ParameterAttributes.Retval) != 0;

        // The member in which the parameter is defined
        public MemberInfo Member { get; }

        // Name of parameter
        public string Name { get; }

        // Type of this parameter
        private readonly Il2CppType paramType;
        public TypeInfo ParameterType => Member.Assembly.Model.GetType(paramType, MemberTypes.TypeInfo);

        // Zero-indexed position of the parameter in parameter list
        public int Position { get; private set; }

        // Create a parameter. Specify paramIndex == -1 for a return type parameter
        public ParameterInfo(Il2CppInspector pkg, int paramIndex, MethodInfo declaringMethod) {
            Member = declaringMethod;

            if (paramIndex == -1) {
                Position = -1;
                paramType = pkg.TypeUsages[declaringMethod.Definition.returnType];
                Attributes |= ParameterAttributes.Retval;
                return;
            }

            var param = pkg.Metadata.Params[paramIndex];
            Name = pkg.Metadata.Strings[param.nameIndex];
            Position = paramIndex - declaringMethod.Definition.parameterStart;
            paramType = pkg.TypeUsages[param.typeIndex];

            if ((paramType.attrs & Il2CppConstants.PARAM_ATTRIBUTE_OPTIONAL) != 0)
                Attributes |= ParameterAttributes.Optional;
            if ((paramType.attrs & Il2CppConstants.PARAM_ATTRIBUTE_OUT) != 0)
                Attributes |= ParameterAttributes.Out;

            if (Position == -1)
                Attributes |= ParameterAttributes.Retval;
            else if (!IsOut)
                Attributes |= ParameterAttributes.In;

            // TODO: DefaultValue/HasDefaultValue
        }
    }
}
