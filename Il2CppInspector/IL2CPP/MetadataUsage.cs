/*
    Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
    Copyright 2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com

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
        public int DestinationIndex { get; }

        public MetadataUsage(MetadataUsageType type, int sourceIndex, int destinationIndex) {
            Type = type;
            SourceIndex = sourceIndex;
            DestinationIndex = destinationIndex;
        }
    }
}