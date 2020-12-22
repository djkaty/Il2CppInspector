/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

// This is the ONLY line to update when the API version changes
using System.IO;
using NoisyCowStudios.Bin2Object;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;

namespace Il2CppInspector
{
    // Hooks we provide to plugins which can choose whether or not to provide implementations
    internal static class PluginHooks
    {
        public static PluginPreProcessMetadataEventInfo PreProcessMetadata(BinaryObjectStream stream)
            => PluginManager.Try<IPreProcessMetadata, PluginPreProcessMetadataEventInfo>((p, e) => {
                    stream.Position = 0;
                    p.PreProcessMetadata(stream, e);
                });

        public static PluginPostProcessMetadataEventInfo PostProcessMetadata(Metadata metadata)
            => PluginManager.Try<IPostProcessMetadata, PluginPostProcessMetadataEventInfo>((p, e) => p.PostProcessMetadata(metadata, e));

        public static PluginGetStringsEventInfo GetStrings(Metadata metadata)
            => PluginManager.Try<IGetStrings, PluginGetStringsEventInfo>((p, e) => p.GetStrings(metadata, e));

        public static PluginGetStringLiteralsEventInfo GetStringLiterals(Metadata metadata)
            => PluginManager.Try<IGetStringLiterals, PluginGetStringLiteralsEventInfo>((p, e) => p.GetStringLiterals(metadata, e));

        public static PluginPostProcessPackageEventInfo PostProcessPackage(Il2CppInspector package)
            => PluginManager.Try<IPostProcessPackage, PluginPostProcessPackageEventInfo>((p, e) => p.PostProcessPackage(package, e));

        public static PluginPostProcessTypeModelEventInfo PostProcessTypeModel(TypeModel typeModel)
            => PluginManager.Try<IPostProcessTypeModel, PluginPostProcessTypeModelEventInfo>((p, e) => p.PostProcessTypeModel(typeModel, e));

    }
}
