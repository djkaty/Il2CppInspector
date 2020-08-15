/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // From class-internals.h / il2cpp-class-internals.h
    public class Il2CppCodeRegistration
    {
        // Moved to Il2CppCodeGenModule in v24.2
        [Version(Max = 24.1)]
        public ulong methodPointersCount;
        [Version(Max = 24.1)]
        public ulong pmethodPointers;

        public ulong reversePInvokeWrapperCount; // (was renamed from delegateWrappersFromNativeToManagedCount in v22)
        public ulong reversePInvokeWrappers; // (was renamed from delegateWrappersFromNativeToManaged in v22)

        // Removed in metadata v23
        [Version(Max = 22)]
        public ulong delegateWrappersFromManagedToNativeCount;
        [Version(Max = 22)]
        public ulong delegateWrappersFromManagedToNative;
        [Version(Max = 22)]
        public ulong marshalingFunctionsCount;
        [Version(Max = 22)]
        public ulong marshalingFunctions;
        [Version(Min = 21, Max = 22)]
        public ulong ccwMarshalingFunctionsCount;
        [Version(Min = 21, Max = 22)]
        public ulong ccwMarshalingFunctions;

        public ulong genericMethodPointersCount;
        public ulong genericMethodPointers;
        public ulong invokerPointersCount;
        public ulong invokerPointers;

        // Removed in metadata v27
        [Version(Max = 24.3)]
        public long customAttributeCount;
        [Version(Max = 24.3)]
        public ulong customAttributeGenerators;

        // Removed in metadata v23
        [Version(Min = 21, Max = 22)]
        public long guidCount;
        [Version(Min = 21, Max = 22)]
        public ulong guids; // Il2CppGuid

        // Added in metadata v22
        [Version(Min = 22)]
        public ulong unresolvedVirtualCallCount;
        [Version(Min = 22)]
        public ulong unresolvedVirtualCallPointers;

        // Added in metadata v23
        [Version(Min = 23)]
        public ulong interopDataCount;
        [Version(Min = 23)]
        public ulong interopData;

        [Version(Min = 24.3)]
        public ulong windowsRuntimeFactoryCount;
        [Version(Min = 24.3)]
        public ulong windowsRuntimeFactoryTable;

        // Added in metadata v24.2 to replace methodPointers and methodPointersCount
        [Version(Min = 24.2)]
        public ulong codeGenModulesCount;
        [Version(Min = 24.2)]
        public ulong pcodeGenModules;
    }

    // Introduced in metadata v24.2 (replaces method pointers in Il2CppCodeRegistration)
    public class Il2CppCodeGenModule
    {
        public ulong moduleName;
        public ulong methodPointerCount;
        public ulong methodPointers;
        public ulong invokerIndices;
        public ulong reversePInvokeWrapperCount;
        public ulong reversePInvokeWrapperIndices;
        public ulong rgctxRangesCount;
        public ulong rgctxRanges;
        public ulong rgctxsCount;
        public ulong rgctxs;
        public ulong debuggerMetadata;

        // Added in metadata v27
        public ulong customAttributeCacheGenerator; // CustomAttributesCacheGenerator*
        public ulong moduleInitializer; // Il2CppMethodPointer
        public ulong staticConstructorTypeIndices; // TypeDefinitionIndex*
        public ulong metadataRegistration; // Il2CppMetadataRegistration* // Per-assembly mode only
        public ulong codeRegistration; // Il2CppCodeRegistration* // Per-assembly mode only
    }

#pragma warning disable CS0649
    public class Il2CppMetadataRegistration
    {
        public long genericClassesCount;
        public ulong genericClasses;
        public long genericInstsCount;
        public ulong genericInsts;
        public long genericMethodTableCount;
        public ulong genericMethodTable; // Il2CppGenericMethodFunctionsDefinitions
        public long typesCount;
        public ulong ptypes;
        public long methodSpecsCount;
        public ulong methodSpecs;
        [Version(Max = 16)]
        public long methodReferencesCount;
        [Version(Max = 16)]
        public ulong methodReferences;

        public long fieldOffsetsCount;
        public ulong pfieldOffsets; // Changed from int32_t* to int32_t** after 5.4.0f3, before 5.5.0f3

        public long typeDefinitionsSizesCount;
        public ulong typeDefinitionsSizes;
        [Version(Min = 19)]
        public ulong metadataUsagesCount;
        [Version(Min = 19)]
        public ulong metadataUsages;
    }
#pragma warning restore CS0649

    // From blob.h / il2cpp-blob.h
    public enum Il2CppTypeEnum
    {
        IL2CPP_TYPE_END = 0x00,       /* End of List */
        IL2CPP_TYPE_VOID = 0x01,
        IL2CPP_TYPE_BOOLEAN = 0x02,
        IL2CPP_TYPE_CHAR = 0x03,
        IL2CPP_TYPE_I1 = 0x04,
        IL2CPP_TYPE_U1 = 0x05,
        IL2CPP_TYPE_I2 = 0x06,
        IL2CPP_TYPE_U2 = 0x07,
        IL2CPP_TYPE_I4 = 0x08,
        IL2CPP_TYPE_U4 = 0x09,
        IL2CPP_TYPE_I8 = 0x0a,
        IL2CPP_TYPE_U8 = 0x0b,
        IL2CPP_TYPE_R4 = 0x0c,
        IL2CPP_TYPE_R8 = 0x0d,
        IL2CPP_TYPE_STRING = 0x0e,
        IL2CPP_TYPE_PTR = 0x0f,       /* arg: <type> token */
        IL2CPP_TYPE_BYREF = 0x10,       /* arg: <type> token */
        IL2CPP_TYPE_VALUETYPE = 0x11,       /* arg: <type> token */
        IL2CPP_TYPE_CLASS = 0x12,       /* arg: <type> token */
        IL2CPP_TYPE_VAR = 0x13,       /* Generic parameter in a generic type definition, represented as number (compressed unsigned integer) number */
        IL2CPP_TYPE_ARRAY = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
        IL2CPP_TYPE_GENERICINST = 0x15,    /* <type> <type-arg-count> <type-1> \x{2026} <type-n> */
        IL2CPP_TYPE_TYPEDBYREF = 0x16,
        IL2CPP_TYPE_I = 0x18,
        IL2CPP_TYPE_U = 0x19,
        IL2CPP_TYPE_FNPTR = 0x1b,         /* arg: full method signature */
        IL2CPP_TYPE_OBJECT = 0x1c,
        IL2CPP_TYPE_SZARRAY = 0x1d,       /* 0-based one-dim-array */
        IL2CPP_TYPE_MVAR = 0x1e,       /* Generic parameter in a generic method definition, represented as number (compressed unsigned integer)  */
        IL2CPP_TYPE_CMOD_REQD = 0x1f,       /* arg: typedef or typeref token */
        IL2CPP_TYPE_CMOD_OPT = 0x20,       /* optional arg: typedef or typref token */
        IL2CPP_TYPE_INTERNAL = 0x21,       /* CLR internal type */

        IL2CPP_TYPE_MODIFIER = 0x40,       /* Or with the following types */
        IL2CPP_TYPE_SENTINEL = 0x41,       /* Sentinel for varargs method signature */
        IL2CPP_TYPE_PINNED = 0x45,       /* Local var that points to pinned object */

        IL2CPP_TYPE_ENUM = 0x55        /* an enumeration */
    }

    // From metadata.h / il2cpp-runtime-metadata.h
    public class Il2CppType
    {
        /*
        union
        {
            TypeDefinitionIndex klassIndex; // for VALUETYPE and CLASS (<v27; v27: at startup)
            Il2CppMetadataTypeHandle typeHandle; // for VALUETYPE and CLASS (added in v27: at runtime)
            const Il2CppType* type; // for PTR and SZARRAY 
            Il2CppArrayType* array; // for ARRAY 
            GenericParameterIndex genericParameterIndex; // for VAR and MVAR (<v27; v27: at startup)
            Il2CppMetadataGenericParameterHandle genericParameterHandle; // for VAR and MVAR (added in v27: at runtime)
            Il2CppGenericClass* generic_class; // for GENERICINST
        }
        */
        public ulong datapoint;
        public ulong bits; // this should be private but we need it to be public for BinaryObjectReader to work

        public uint attrs => (uint) bits & 0xffff; /* param attributes or field flags */
        public Il2CppTypeEnum type => (Il2CppTypeEnum)((bits >> 16) & 0xff);
        public uint num_mods => (uint) (bits >> 24) & 0x3f; /* max 64 modifiers follow at the end */
        public bool byref => ((bits >> 30) & 1) == 1;
        public bool pinned => (bits >> 31) == 1; /* valid when included in a local var signature */
    }

    public class Il2CppGenericClass
    {
        [Version(Max = 24.3)]
        public long typeDefinitionIndex;    /* the generic type definition */
        [Version(Min = 27)]
        public ulong type; // Il2CppType*   /* the generic type definition */

        public Il2CppGenericContext context;   /* a context that contains the type instantiation doesn't contain any method instantiation */
        public ulong cached_class; /* if present, the Il2CppClass corresponding to the instantiation.  */
    }

    public class Il2CppGenericContext
    {
        /* The instantiation corresponding to the class generic parameters */
        public ulong class_inst;
        /* The instantiation corresponding to the method generic parameters */
        public ulong method_inst;
    }

    public class Il2CppGenericInst
    {
        public ulong type_argc;
        public ulong type_argv;
    }

    public class Il2CppArrayType
    {
        public ulong etype;
        public byte rank;
        public byte numsizes;
        public byte numlobounds;
        public ulong sizes;
        public ulong lobounds;
    }

    public class Il2CppMethodSpec
    {
        public int methodDefinitionIndex;
        public int classIndexIndex;
        public int methodIndexIndex;
    }

    public class Il2CppGenericMethodFunctionsDefinitions
    {
        public int genericMethodIndex;
        public Il2CppGenericMethodIndices indices;
    }

    public class Il2CppGenericMethodIndices
    {
        public int methodIndex;
        public int invokerIndex;
    }
}
