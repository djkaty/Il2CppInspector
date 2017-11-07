using System.Collections.Generic;

public static class DefineConstants
{
    public const int FIELD_ATTRIBUTE_PRIVATE = 0x0001;
    public const int FIELD_ATTRIBUTE_PUBLIC = 0x0006;
    public const int FIELD_ATTRIBUTE_STATIC = 0x0010;
    public const int FIELD_ATTRIBUTE_INIT_ONLY = 0x0020;
    public const int METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK = 0x0007;
    public const int METHOD_ATTRIBUTE_PRIVATE = 0x0001;
    public const int METHOD_ATTRIBUTE_PUBLIC = 0x0006;
    public const int METHOD_ATTRIBUTE_STATIC = 0x0010;
    public const int METHOD_ATTRIBUTE_VIRTUAL = 0x0040;
    public const int TYPE_ATTRIBUTE_VISIBILITY_MASK = 0x00000007;
    public const int TYPE_ATTRIBUTE_PUBLIC = 0x00000001;
    public const int TYPE_ATTRIBUTE_INTERFACE = 0x00000020;
    public const int TYPE_ATTRIBUTE_ABSTRACT = 0x00000080;
    public const int TYPE_ATTRIBUTE_SEALED = 0x00000100;
    public const int TYPE_ATTRIBUTE_SERIALIZABLE = 0x00002000;
    public const int PARAM_ATTRIBUTE_OUT = 0x0002;
    public const int PARAM_ATTRIBUTE_OPTIONAL = 0x0010;

    public static List<string> CSharpTypeString = new List<string>
    {
        "END",
        "void",
        "bool",
        "char",
        "sbyte",
        "byte",
        "short",
        "ushort",
        "int",
        "uint",
        "long",
        "ulong",
        "float",
        "double",
        "string",
        "PTR",          // Processed separately
        "BYREF",
        "VALUETYPE",    // Processed separately
        "CLASS",        // Processed separately
        "T",
        "Array",        // Processed separately
        "GENERICINST",  // Processed separately
        "TYPEDBYREF",
        "None",
        "IntPtr",
        "UIntPtr",
        "None",
        "delegate",
        "object",
        "SZARRAY",      // Processed separately
        "T",
        "CMOD_REQD",
        "CMOD_OPT",
        "INTERNAL",
    };

    public static List<string> FullNameTypeString = new List<string>
    {
        "END",
        "System.Void",
        "System.Boolean",
        "System.Char",
        "System.SByte",
        "System.Byte",
        "System.Int16",
        "System.UInt16",
        "System.Int32",
        "System.UInt32",
        "System.Int64",
        "System.UInt64",
        "System.Single",
        "System.Double",
        "System.String",
        "PTR",                 // Processed separately
        "BYREF",
        "System.ValueType",    // Processed separately
        "CLASS",               // Processed separately
        "T",
        "System.Array",        // Processed separately
        "GENERICINST",         // Processed separately
        "TYPEDBYREF",
        "None",
        "System.IntPtr",
        "System.UIntPtr",
        "None",
        "System.Delegate",
        "System.Object",
        "SZARRAY",             // Processed separately
        "T",
        "CMOD_REQD",
        "CMOD_OPT",
        "INTERNAL",
    };
}