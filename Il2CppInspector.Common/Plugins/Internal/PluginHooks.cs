/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections.Generic;
using System.IO;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using NoisyCowStudios.Bin2Object;

// This is the ONLY line to update when the API version changes
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector
{
    // Internal helpers to call the same hook on every plugin
    // Does not include hooks that should be called individually, eg. OptionsChanged
    // NOTE: The method names must be identical to the interface method names for stack tracing to work
    internal static class PluginHooks
    {
        public static PluginLoadPipelineStartingEventInfo LoadPipelineStarting()
            => PluginManager.Try<ILoadPipeline, PluginLoadPipelineStartingEventInfo>((p, e) => p.LoadPipelineStarting(e));

        public static PluginPreProcessMetadataEventInfo PreProcessMetadata(BinaryObjectStream stream)
            => PluginManager.Try<ILoadPipeline, PluginPreProcessMetadataEventInfo>((p, e) => {
                    stream.Position = 0;
                    p.PreProcessMetadata(stream, e);
                });

        public static PluginPostProcessMetadataEventInfo PostProcessMetadata(Metadata metadata)
            => PluginManager.Try<ILoadPipeline, PluginPostProcessMetadataEventInfo>((p, e) => p.PostProcessMetadata(metadata, e));

        public static PluginGetStringsEventInfo GetStrings(Metadata metadata)
            => PluginManager.Try<ILoadPipeline, PluginGetStringsEventInfo>((p, e) => p.GetStrings(metadata, e));

        public static PluginGetStringLiteralsEventInfo GetStringLiterals(Metadata metadata)
            => PluginManager.Try<ILoadPipeline, PluginGetStringLiteralsEventInfo>((p, e) => p.GetStringLiterals(metadata, e));

        public static PluginPreProcessImageEventInfo PreProcessImage(BinaryObjectStream stream)
            => PluginManager.Try<ILoadPipeline, PluginPreProcessImageEventInfo>((p, e) => p.PreProcessImage(stream, e));

        public static PluginPostProcessImageEventInfo PostProcessImage<T>(FileFormatStream<T> stream) where T : FileFormatStream<T>
            => PluginManager.Try<ILoadPipeline, PluginPostProcessImageEventInfo>((p, e) => p.PostProcessImage(stream, e));

        public static PluginPreProcessBinaryEventInfo PreProcessBinary(Il2CppBinary binary)
            => PluginManager.Try<ILoadPipeline, PluginPreProcessBinaryEventInfo>((p, e) => p.PreProcessBinary(binary, e));

        public static PluginPostProcessBinaryEventInfo PostProcessBinary(Il2CppBinary binary)
            => PluginManager.Try<ILoadPipeline, PluginPostProcessBinaryEventInfo>((p, e) => p.PostProcessBinary(binary, e));

        public static PluginPostProcessPackageEventInfo PostProcessPackage(Il2CppInspector package)
            => PluginManager.Try<ILoadPipeline, PluginPostProcessPackageEventInfo>((p, e) => p.PostProcessPackage(package, e));

        public static PluginLoadPipelineEndingEventInfo LoadPipelineEnding(List<Il2CppInspector> packages)
            => PluginManager.Try<ILoadPipeline, PluginLoadPipelineEndingEventInfo>((p, e) => p.LoadPipelineEnding(packages, e));

        public static PluginPostProcessTypeModelEventInfo PostProcessTypeModel(TypeModel typeModel)
            => PluginManager.Try<ILoadPipeline, PluginPostProcessTypeModelEventInfo>((p, e) => p.PostProcessTypeModel(typeModel, e));

        public static PluginPostProcessAppModelEventInfo PostProcessAppModel(AppModel appModel)
            => PluginManager.Try<ILoadPipeline, PluginPostProcessAppModelEventInfo>((p, e) => p.PostProcessAppModel(appModel, e));
    }
}
