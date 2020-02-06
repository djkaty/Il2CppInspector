using System.Collections.Generic;

// Constants from il2cpp/tabledefs.h

namespace Il2CppInspector
{
    public static class Il2CppConstants
    {
        /*
         * Field Attributes (21.1.5).
         */

        public const int FIELD_ATTRIBUTE_FIELD_ACCESS_MASK = 0x0007;
        public const int FIELD_ATTRIBUTE_COMPILER_CONTROLLED = 0x0000;
        public const int FIELD_ATTRIBUTE_PRIVATE = 0x0001;
        public const int FIELD_ATTRIBUTE_FAM_AND_ASSEM = 0x0002;
        public const int FIELD_ATTRIBUTE_ASSEMBLY = 0x0003;
        public const int FIELD_ATTRIBUTE_FAMILY = 0x0004;
        public const int FIELD_ATTRIBUTE_FAM_OR_ASSEM = 0x0005;
        public const int FIELD_ATTRIBUTE_PUBLIC = 0x0006;

        public const int FIELD_ATTRIBUTE_STATIC = 0x0010;
        public const int FIELD_ATTRIBUTE_INIT_ONLY = 0x0020;
        public const int FIELD_ATTRIBUTE_LITERAL = 0x0040;
        public const int FIELD_ATTRIBUTE_NOT_SERIALIZED = 0x0080;
        public const int FIELD_ATTRIBUTE_SPECIAL_NAME = 0x0200;
        public const int FIELD_ATTRIBUTE_PINVOKE_IMPL = 0x2000;

        /* For runtime use only */
        public const int FIELD_ATTRIBUTE_RESERVED_MASK = 0x9500;

        public const int FIELD_ATTRIBUTE_RT_SPECIAL_NAME = 0x0400;
        public const int FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL = 0x1000;
        public const int FIELD_ATTRIBUTE_HAS_DEFAULT = 0x8000;
        public const int FIELD_ATTRIBUTE_HAS_FIELD_RVA = 0x0100;

        /*
         * Method Attributes (22.1.9)
         */

        public const int METHOD_IMPL_ATTRIBUTE_CODE_TYPE_MASK = 0x0003;
        public const int METHOD_IMPL_ATTRIBUTE_IL = 0x0000;
        public const int METHOD_IMPL_ATTRIBUTE_NATIVE = 0x0001;
        public const int METHOD_IMPL_ATTRIBUTE_OPTIL = 0x0002;
        public const int METHOD_IMPL_ATTRIBUTE_RUNTIME = 0x0003;

        public const int METHOD_IMPL_ATTRIBUTE_MANAGED_MASK = 0x0004;
        public const int METHOD_IMPL_ATTRIBUTE_UNMANAGED = 0x0004;
        public const int METHOD_IMPL_ATTRIBUTE_MANAGED = 0x0000;

        public const int METHOD_IMPL_ATTRIBUTE_FORWARD_REF = 0x0010;
        public const int METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG = 0x0080;
        public const int METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL = 0x1000;
        public const int METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED = 0x0020;
        public const int METHOD_IMPL_ATTRIBUTE_NOINLINING = 0x0008;
        public const int METHOD_IMPL_ATTRIBUTE_MAX_METHOD_IMPL_VAL = 0xffff;

        public const int METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK = 0x0007;
        public const int METHOD_ATTRIBUTE_COMPILER_CONTROLLED = 0x0000;
        public const int METHOD_ATTRIBUTE_PRIVATE = 0x0001;
        public const int METHOD_ATTRIBUTE_FAM_AND_ASSEM = 0x0002;
        public const int METHOD_ATTRIBUTE_ASSEM = 0x0003;
        public const int METHOD_ATTRIBUTE_FAMILY = 0x0004;
        public const int METHOD_ATTRIBUTE_FAM_OR_ASSEM = 0x0005;
        public const int METHOD_ATTRIBUTE_PUBLIC = 0x0006;

        public const int METHOD_ATTRIBUTE_STATIC = 0x0010;
        public const int METHOD_ATTRIBUTE_FINAL = 0x0020;
        public const int METHOD_ATTRIBUTE_VIRTUAL = 0x0040;
        public const int METHOD_ATTRIBUTE_HIDE_BY_SIG = 0x0080;

        public const int METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK = 0x0100;
        public const int METHOD_ATTRIBUTE_REUSE_SLOT = 0x0000;
        public const int METHOD_ATTRIBUTE_NEW_SLOT = 0x0100;

        public const int METHOD_ATTRIBUTE_STRICT = 0x0200;
        public const int METHOD_ATTRIBUTE_ABSTRACT = 0x0400;
        public const int METHOD_ATTRIBUTE_SPECIAL_NAME = 0x0800;

        public const int METHOD_ATTRIBUTE_PINVOKE_IMPL = 0x2000;
        public const int METHOD_ATTRIBUTE_UNMANAGED_EXPORT = 0x0008;

        /*
         * For runtime use only
         */
        public const int METHOD_ATTRIBUTE_RESERVED_MASK = 0xd000;
        public const int METHOD_ATTRIBUTE_RT_SPECIAL_NAME = 0x1000;
        public const int METHOD_ATTRIBUTE_HAS_SECURITY = 0x4000;
        public const int METHOD_ATTRIBUTE_REQUIRE_SEC_OBJECT = 0x8000;

        /*
        * Type Attributes (21.1.13).
        */
        public const int TYPE_ATTRIBUTE_VISIBILITY_MASK = 0x00000007;
        public const int TYPE_ATTRIBUTE_NOT_PUBLIC = 0x00000000;
        public const int TYPE_ATTRIBUTE_PUBLIC = 0x00000001;
        public const int TYPE_ATTRIBUTE_NESTED_PUBLIC = 0x00000002;
        public const int TYPE_ATTRIBUTE_NESTED_PRIVATE = 0x00000003;
        public const int TYPE_ATTRIBUTE_NESTED_FAMILY = 0x00000004;
        public const int TYPE_ATTRIBUTE_NESTED_ASSEMBLY = 0x00000005;
        public const int TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM = 0x00000006;
        public const int TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM = 0x00000007;

        public const int TYPE_ATTRIBUTE_LAYOUT_MASK = 0x00000018;
        public const int TYPE_ATTRIBUTE_AUTO_LAYOUT = 0x00000000;
        public const int TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT = 0x00000008;
        public const int TYPE_ATTRIBUTE_EXPLICIT_LAYOUT = 0x00000010;

        public const int TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK = 0x00000020;
        public const int TYPE_ATTRIBUTE_CLASS = 0x00000000;
        public const int TYPE_ATTRIBUTE_INTERFACE = 0x00000020;

        public const int TYPE_ATTRIBUTE_ABSTRACT = 0x00000080;
        public const int TYPE_ATTRIBUTE_SEALED = 0x00000100;
        public const int TYPE_ATTRIBUTE_SPECIAL_NAME = 0x00000400;

        public const int TYPE_ATTRIBUTE_IMPORT = 0x00001000;
        public const int TYPE_ATTRIBUTE_SERIALIZABLE = 0x00002000;

        public const int TYPE_ATTRIBUTE_STRING_FORMAT_MASK = 0x00030000;
        public const int TYPE_ATTRIBUTE_ANSI_CLASS = 0x00000000;
        public const int TYPE_ATTRIBUTE_UNICODE_CLASS = 0x00010000;
        public const int TYPE_ATTRIBUTE_AUTO_CLASS = 0x00020000;

        public const int TYPE_ATTRIBUTE_BEFORE_FIELD_INIT = 0x00100000;
        public const int TYPE_ATTRIBUTE_FORWARDER = 0x00200000;

        public const int TYPE_ATTRIBUTE_RESERVED_MASK = 0x00040800;
        public const int TYPE_ATTRIBUTE_RT_SPECIAL_NAME = 0x00000800;
        public const int TYPE_ATTRIBUTE_HAS_SECURITY = 0x00040000;

        /*
        * Flags for Params (22.1.12)
        */
        public const int PARAM_ATTRIBUTE_IN = 0x0001;
        public const int PARAM_ATTRIBUTE_OUT = 0x0002;
        public const int PARAM_ATTRIBUTE_OPTIONAL = 0x0010;
        public const int PARAM_ATTRIBUTE_RESERVED_MASK = 0xf000;
        public const int PARAM_ATTRIBUTE_HAS_DEFAULT = 0x1000;
        public const int PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL = 0x2000;
        public const int PARAM_ATTRIBUTE_UNUSED = 0xcfe0;

        // Flags for Generic Parameters (II.23.1.7)
        public const int GENERIC_PARAMETER_ATTRIBUTE_NON_VARIANT = 0x00;
        public const int GENERIC_PARAMETER_ATTRIBUTE_COVARIANT = 0x01;
        public const int GENERIC_PARAMETER_ATTRIBUTE_CONTRAVARIANT = 0x02;
        public const int GENERIC_PARAMETER_ATTRIBUTE_VARIANCE_MASK = 0x03;
        public const int GENERIC_PARAMETER_ATTRIBUTE_REFERENCE_TYPE_CONSTRAINT = 0x04;
        public const int GENERIC_PARAMETER_ATTRIBUTE_NOT_NULLABLE_VALUE_TYPE_CONSTRAINT = 0x08;
        public const int GENERIC_PARAMETER_ATTRIBUTE_DEFAULT_CONSTRUCTOR_CONSTRAINT = 0x10;
        public const int GENERIC_PARAMETER_ATTRIBUTE_SPECIAL_CONSTRAINT_MASK = 0x1C;

        /**
         * 21.5 AssemblyRefs
         */
        public const int ASSEMBLYREF_FULL_PUBLIC_KEY_FLAG = 0x00000001;
        public const int ASSEMBLYREF_RETARGETABLE_FLAG = 0x00000100;
        public const int ASSEMBLYREF_ENABLEJITCOMPILE_TRACKING_FLAG = 0x00008000;
        public const int ASSEMBLYREF_DISABLEJITCOMPILE_OPTIMIZER_FLAG = 0x00004000;

        // Naming conventions (follows order of Il2CppTypeEnum)
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
            "ValueType",    // Processed separately
            "CLASS",        // Processed separately
            "T",
            "Array",        // Processed separately
            "GENERICINST",  // Processed separately
            "TypedReference", // params
            "None",
            "IntPtr",
            "UIntPtr",
            "None",
            "Delegate",
            "object",
            "SZARRAY",      // Processed separately
            "T",
            "CMOD_REQD",
            "CMOD_OPT",
            "INTERNAL",

            // Added in for convenience
            "decimal"
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
            "System.TypedReference", // params
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

            // Added in for convenience
            "System.Decimal"
        };
    }
}