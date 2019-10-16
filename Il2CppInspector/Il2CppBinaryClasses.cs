/*
    Copyright 2017 Perfare - https://github.com/Perfare/Il2CppDumper
    Copyright 2017-2019 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;

namespace Il2CppInspector
{
    // From class-internals.h / il2cpp-class-internals.h
    public class Il2CppCodeRegistration
    {
        // Moved to Il2CppCodeGenModule in later versions of v24
        [Version(Max = 24.0)]
        public uint methodPointersCount;
        [Version(Max = 24.0)]
        public uint pmethodPointers;

        public uint reversePInvokeWrapperCount; // (was renamed from delegateWrappersFromNativeToManagedCount in v22)
        public uint reversePInvokeWrappers; // (was renamed from delegateWrappersFromNativeToManaged in v22)

        // Removed in metadata v23
        [Version(Max = 22)]
        public uint delegateWrappersFromManagedToNativeCount;
        [Version(Max = 22)]
        public uint delegateWrappersFromManagedToNative;
        [Version(Max = 22)]
        public uint marshalingFunctionsCount;
        [Version(Max = 22)]
        public uint marshalingFunctions;
        [Version(Max = 22)]
        public uint ccwMarshalingFunctionsCount;
        [Version(Max = 22)]
        public uint ccwMarshalingFunctions;

        public uint genericMethodPointersCount;
        public uint genericMethodPointers;
        public uint invokerPointersCount;
        public uint invokerPointers;
        public int customAttributeCount;
        public uint customAttributeGenerators;

        // Removed in metadata v23
        [Version(Max = 22)]
        public int guidCount;
        [Version(Max = 22)]
        public uint guids; // Il2CppGuid

        // Added in metadata v22
        [Version(Min = 22)]
        public uint unresolvedVirtualCallCount;
        [Version(Min = 22)]
        public uint unresolvedVirtualCallPointers;

        // Added in metadata v23
        [Version(Min = 23)]
        public uint interopDataCount;
        [Version(Min = 23)]
        public uint interopData;

        // Added in later versions of metadata v24
        [Version(Min = 24.1)]
        public uint codeGenModulesCount;
        [Version(Min = 24.1)]
        public uint pcodeGenModules;
    }

    // Introduced in metadata v24.1 (replaces method pointers in Il2CppCodeRegistration)
    public class Il2CppCodeGenModule
    {
        public uint moduleName;
        public uint methodPointerCount;
        public uint methodPointers;
        public uint invokerIndices;
        public uint reversePInvokeWrapperCount;
        public uint reversePInvokeWrapperIndices;
        public uint rgctxRangesCount;
        public uint rgctxRanges;
        public uint rgctxsCount;
        public uint rgctxs;
        public uint debuggerMetadata;
    }

#pragma warning disable CS0649
    public class Il2CppMetadataRegistration
    {
        public int genericClassesCount;
        public uint genericClasses;
        public int genericInstsCount;
        public uint genericInsts;
        public int genericMethodTableCount;
        public uint genericMethodTable; // Il2CppGenericMethodFunctionsDefinitions
        public int typesCount;
        public uint ptypes;
        public int methodSpecsCount;
        public uint methodSpecs;

        public int fieldOffsetsCount;
        public uint pfieldOffsets;

        public int typeDefinitionsSizesCount;
        public uint typeDefinitionsSizes;
        public uint metadataUsagesCount;
        public uint metadataUsages;
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
            TypeDefinitionIndex klassIndex; // for VALUETYPE and CLASS 
            const Il2CppType* type; // for PTR and SZARRAY 
            Il2CppArrayType* array; // for ARRAY 
            GenericParameterIndex genericParameterIndex; // for VAR and MVAR 
            Il2CppGenericClass* generic_class; // for GENERICINST
        }
        */
        public uint datapoint;
        public uint bits; // this should be private but we need it to be public for BinaryObjectReader to work

        public uint attrs => bits & 0xffff; /* param attributes or field flags */
        public Il2CppTypeEnum type => (Il2CppTypeEnum)((bits >> 16) & 0xff);
        public uint num_mods => (bits >> 24) & 0x3f; /* max 64 modifiers follow at the end */
        public bool byref => ((bits >> 30) & 1) == 1;
        public bool pinned => (bits >> 31) == 1; /* valid when included in a local var signature */
    }

    public class Il2CppGenericClass
    {
        public int typeDefinitionIndex;    /* the generic type definition */
        public Il2CppGenericContext context;   /* a context that contains the type instantiation doesn't contain any method instantiation */
        public uint cached_class; /* if present, the Il2CppClass corresponding to the instantiation.  */
    }

    public class Il2CppGenericContext
    {
        /* The instantiation corresponding to the class generic parameters */
        public uint class_inst;
        /* The instantiation corresponding to the method generic parameters */
        public uint method_inst;
    }

    public class Il2CppGenericInst
    {
        public uint type_argc;
        public uint type_argv;
    }

    public class Il2CppArrayType
    {
        public uint etype;
        public byte rank;
        public byte numsizes;
        public byte numlobounds;
        public uint sizes;
        public uint lobounds;
    }
}
