/*
    Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
    Copyright (c) 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
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
        public uint DestinationIndex { get; }
        public ulong VirtualAddress { get; private set; }

        public MetadataUsage(MetadataUsageType type, int sourceIndex, uint destinationIndex, ulong virtualAddress = 0)
        {
            Type = type;
            SourceIndex = sourceIndex;
            VirtualAddress = virtualAddress;
            DestinationIndex = destinationIndex;
        }

        public static MetadataUsage FromEncodedIndex(Il2CppInspector package, uint encodedIndex,
            ulong virtualAddress = 0)
        {
            uint index;
            MetadataUsageType usageType;
            if (package.Version < 19)
            {
                /* These encoded indices appear only in vtables, and are decoded by IsGenericMethodIndex/GetDecodedMethodIndex */
                var isGeneric = encodedIndex & 0x80000000;
                index = package.Binary.VTableMethodReferences[encodedIndex & 0x7FFFFFFF];
                usageType = (isGeneric != 0) ? MetadataUsageType.MethodRef : MetadataUsageType.MethodDef;
            }
            else
            {
                /* These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
                var encodedType = encodedIndex & 0xE0000000;
                usageType = (MetadataUsageType)(encodedType >> 29);
                index = encodedIndex & 0x1FFFFFFF;

                // From v27 the bottom bit is set to indicate the usage token hasn't been replaced with a pointer at runtime yet
                if (package.Version >= 27)
                    index >>= 1;
            }

            return new MetadataUsage(usageType, (int)index, 0, virtualAddress);
        }

        public static MetadataUsage FromUsagePairMihoyo(Il2CppInspector package, Il2CppMetadataUsagePair usagePair, ulong virtualAddress = 0)
        {
            ulong mihoyoUsageVA = 0x18813CA70;
            var mihoyoUsage = package.Binary.Image.ReadMappedObject<MihoyoUsages>(mihoyoUsageVA);

            uint index;
            MetadataUsageType usageType;

            /* These encoded indices appear in metadata usages, and are decoded by GetEncodedIndexType/GetDecodedMethodIndex */
            var encodedType = usagePair.encodedSourceIndex & 0xE0000000;
            usageType = (MetadataUsageType)(encodedType >> 29);
            index = usagePair.encodedSourceIndex & 0x1FFFFFFF;

            uint destinationIndex = usagePair.destinationindex;
            ulong baseAddress = 0;
            switch (usageType)
            {
                case MetadataUsageType.StringLiteral:
                    destinationIndex += (uint)mihoyoUsage.fieldInfoUsageCount
                                        + (uint)mihoyoUsage.methodDefRefUsageCount
                                        + (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.stringLiteralUsage;
                    break;
                case MetadataUsageType.FieldInfo:
                    destinationIndex += (uint)mihoyoUsage.methodDefRefUsageCount
                                        + (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.fieldInfoUsage;
                    break;
                case MetadataUsageType.MethodDef:
                case MetadataUsageType.MethodRef:
                    destinationIndex += (uint)mihoyoUsage.typeInfoUsageCount;
                    baseAddress = mihoyoUsage.methodDefRefUsage;
                    break;
                case MetadataUsageType.TypeInfo:
                case MetadataUsageType.Type:
                    baseAddress = mihoyoUsage.typeInfoUsage;
                    break;
            }

            virtualAddress = baseAddress + 8 * index;

            return new MetadataUsage(usageType, (int)index, destinationIndex, virtualAddress);

        }

        public void SetAddress(ulong virtualAddress) => VirtualAddress = virtualAddress;
    }
}