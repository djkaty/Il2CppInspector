using System;
using System.Reflection;

namespace Il2CppInspector.Reflection
{
    public abstract class MethodBase : MemberInfo
    {
        // (not code attributes)
        public MethodAttributes Attributes { get; set; }

        // TODO: ContainsGenericParameters
    }

    public class ConstructorInfo : MethodBase
    {
        // TODO
    }

    public class MethodInfo : MethodBase
    {
        // TODO
    }

    public class FieldInfo : MemberInfo
    {
        // TODO
    }

    public class PropertyInfo : MemberInfo
    {
        // TODO
    }

    public class CustomAttributeData
    {
        // TODO
    }
}
