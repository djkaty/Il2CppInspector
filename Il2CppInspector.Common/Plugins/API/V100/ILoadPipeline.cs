/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using NoisyCowStudios.Bin2Object;
using Il2CppInspector.Reflection;

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
        /// Post-process the .NET type model to make changes after it has been fully created
        /// </summary>
        void PostProcessTypeModel(TypeModel model, PluginPostProcessTypeModelEventInfo data) { }
    }
}
