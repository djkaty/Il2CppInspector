/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.IO;

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Process global-metadata.dat when it is first opened as a sequence of bytes
    /// Seek cursor will be at the start of the file
    /// </summary>
    public interface IPreProcessMetadata
    {
        void PreProcessMetadata(MemoryStream stream, PluginPreProcessMetadataEventInfo data);
    }

    /// <summary>
    /// Process global-metadata.dat after it has been loaded into a Metadata object
    /// </summary>
    public interface IPostProcessMetadata
    {
        void PostProcessMetadata(Metadata metadata, PluginPostProcessMetadataEventInfo data);
    }
}
