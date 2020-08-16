/*
    Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
    Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

namespace Il2CppInspector
{
    public enum MetadataUsageType
    {
        TypeInfo = 1,
        Type = 2,
        MethodDef = 3,
        FieldInfo = 4,
        StringLiteral = 5,
        MethodRef = 6,
    }

    public class MetadataUsage
    {
        public MetadataUsageType Type { get; }
        public int SourceIndex { get; }
        public ulong VirtualAddress { get; private set; }

        public MetadataUsage(MetadataUsageType type, int sourceIndex, ulong virtualAddress = 0) {
            Type = type;
            SourceIndex = sourceIndex;
            VirtualAddress = virtualAddress;
        }

        public static MetadataUsage FromEncodedIndex(Il2CppInspector package, uint encodedIndex, ulong virtualAddress = 0) {
            uint index;
            MetadataUsageType usageType;
            if (package.Version < 19) {
                /* These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */
                var isGeneric = encodedIndex & 0x80000000;
                index = package.Binary.VTableMethodReferences[encodedIndex & 0x7FFFFFFF];
                usageType = (isGeneric != 0) ? MetadataUsageType.MethodRef : MetadataUsageType.MethodDef;
            } else {
                /* These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
                var encodedType = encodedIndex & 0xE0000000;
                usageType = (MetadataUsageType)(encodedType >> 29);
                index = encodedIndex & 0x1FFFFFFF;

                // From v27 the bottom bit is set to indicate the usage token hasn't been replaced with a pointer at runtime yet
                if (package.Version >= 27)
                    index >>= 1;
            }
            return new MetadataUsage(usageType, (int)index, virtualAddress);
        }

        public void SetAddress(ulong virtualAddress) => VirtualAddress = virtualAddress;
    }
}