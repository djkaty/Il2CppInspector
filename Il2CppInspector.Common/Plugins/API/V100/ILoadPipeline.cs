/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;
using Il2CppInspector.Reflection;
using System.Collections.Generic;

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Plugins which implement ILoadPipeline perform additional processing on IL2CPP workloads
    /// as they are analyzed and translated into an internal model
    /// Calls are performed in the order listed below
    /// </summary>
    public interface ILoadPipeline
    {
        /// <summary>
        /// A new load task is about to start. Perform per-load initialization here
        /// </summary>
        void LoadPipelineStarting(PluginLoadPipelineStartingEventInfo info) { }

        /// <summary>
        /// Process global-metadata.dat when it is first opened as a sequence of bytes
        /// Seek cursor will be at the start of the file
        /// </summary>
        void PreProcessMetadata(BinaryObjectStream stream, PluginPreProcessMetadataEventInfo data) { }

        /// <summary>
        /// Fetch all of the .NET identifier strings
        /// </summary>
        void GetStrings(Metadata metadata, PluginGetStringsEventInfo data) { }

        /// <summary>
        /// Fetch all of the (constant) string literals
        /// </summary>
        void GetStringLiterals(Metadata metadata, PluginGetStringLiteralsEventInfo data) { }

        /// <summary>
        /// Process global-metadata.dat after it has been loaded into a Metadata object
        /// </summary>
        void PostProcessMetadata(Metadata metadata, PluginPostProcessMetadataEventInfo data) { }

        /// <summary>
        /// Post-process the entire IL2CPP application package after the metadata and binary have been loaded and merged
        /// </summary>
        void PostProcessPackage(Il2CppInspector package, PluginPostProcessPackageEventInfo data) { }

        /// <summary>
        /// The current load task has finished. Perform per-load teardown code here
        /// One Il2CppInspector per sub-image in the binary is supplied
        /// We have closed all open files but the .NET type model has not been created yet
        /// </summary>
        void LoadPipelineEnding(List<Il2CppInspector> packages, PluginLoadPipelineEndingEventInfo info) { }

        /// <summary>
        /// Post-process the .NET type model to make changes after it has been fully created
        /// </summary>
        void PostProcessTypeModel(TypeModel model, PluginPostProcessTypeModelEventInfo data) { }
    }
}
