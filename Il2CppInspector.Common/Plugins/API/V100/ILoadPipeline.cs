/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections.Generic;
using System.IO;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using NoisyCowStudios.Bin2Object;

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
        /// Process the binary image before its format is detected or analyzed
        /// For AAB, APK, split APK and ZIP files, this will be a generated zip file with just the IL2CPP binaries in their original paths
        /// For Fat Mach-O (UB) files with multiple images, this will be the entire file
        /// For Linux process maps (eg. GameGuardian dumps), this will be the maps.txt file
        /// For all other files, this will be the IL2CPP binary image itself
        /// The file is copied to memory before calling this function so you can make modifications without changing the original file(s)
        /// This will be called once per load pipeline
        /// </summary>
        void PreProcessImage(BinaryObjectStream stream, PluginPreProcessImageEventInfo data) { }

        /// <summary>
        /// Process the binary image after format detection and loading but before it is analyzed
        /// This will be one of the classes from the FileFormatStreams folder, ie. ElfReader32, PEReader etc.
        /// This will be called once per IL2CPP binary image, so for multi-architecture images such as multi-targeting APKs and Fat Mach-Os,
        /// you will receive multiple calls
        /// </summary>
        void PostProcessImage<T>(FileFormatStream<T> stream, PluginPostProcessImageEventInfo data) where T : FileFormatStream<T> { }

        /// <summary>
        /// Process the IL2CPP binary after Il2CppCodeRegistration and Il2CppMetadataRegistration have been found
        /// but before any other structures are loaded
        /// Called once per IL2CPP binary image
        /// </summary>
        void PreProcessBinary(Il2CppBinary binary, PluginPreProcessBinaryEventInfo data) { }

        /// <summary>
        /// Process the IL2CPP binary after all structures have been loaded
        /// Called once per IL2CPP binary image
        /// </summary>
        void PostProcessBinary(Il2CppBinary binary, PluginPostProcessBinaryEventInfo data) { }

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

        /// <summary>
        /// Post-process a C++ application model to make changes after it has been fully created
        /// </summary>
        void PostProcessAppModel(AppModel appModel, PluginPostProcessAppModelEventInfo data) { }
    }
}
