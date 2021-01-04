/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Object which allows plugins to report on what has happened during a call
    /// Changes made to this object propagate to the next plugin in the call chain until FullyProcessed is set to true
    /// </summary>
    public class PluginEventInfo
    {
        /// <summary>
        /// A plugin should set this if it has processed the supplied data in such a way that no further processing is required by other plugins
        /// Generally, this will prevent other plugins from processing the data
        /// Note that this should be set even if the processed data was invalid (<seealso cref="IsInvalid"/>)
        /// If this is not set, the same event will be raised on the next available plugin
        /// Note that you can still do processing but set FullyProcessed to false to allow additional processing from other plugins
        /// </summary>
        public bool FullyProcessed { get; set; } = false;

        /// <summary>
        /// A plugin should set this when the data it processed was invalid, for example if the processing gave an unexpected result
        /// </summary>
        public bool IsInvalid { get; set; } = false;

        /// <summary>
        /// A plugin should set this when it has directly modified the provided data structure (object)
        /// This can be set even if FullyProcessed = false to indicate that changes have been made but more plugins can still be called
        /// Should be set to false if you have only queried (performed reads) on the data without changing it
        /// </summary>
        public bool IsDataModified { get; set; } = false;

        /// <summary>
        /// A plugin should set this when it has directly modified the supplied or underlying stream for the metadata or binary
        /// This can be set even if FullyProcessed = false to indicate that changes have been made but more plugins can still be called
        /// Should be set to false if you have only queried (performed reads) on the stream without changing it
        /// </summary>
        public bool IsStreamModified { get; set; } = false;

        /// <summary>
        /// This wiil be set automatically by Il2CppInspector to the last exception thrown by a plugin for the current event
        /// </summary>
        public PluginErrorEventArgs Error { get; set; } = null;
    }

    /// <summary>
    /// Event info for OptionsChanged
    /// </summary>
    public class PluginOptionsChangedEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for LoadPipelineStarting
    /// </summary>
    public class PluginLoadPipelineStartingEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PreProcessMetadata
    /// </summary>
    public class PluginPreProcessMetadataEventInfo : PluginEventInfo
    {
        /// <summary>
        /// Set to true to disable some validation checks by Il2CppInspector that the metadata is valid
        /// </summary>
        public bool SkipValidation { get; set; }
    }

    /// <summary>
    /// Event info for PostProcessMetadata
    /// </summary>
    public class PluginPostProcessMetadataEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for GetStrings
    /// </summary>
    public class PluginGetStringsEventInfo : PluginEventInfo
    {
        /// <summary>
        /// All of the fetched strings to be returned
        /// </summary>
        public Dictionary<int, string> Strings { get; set; } = new Dictionary<int, string>();
    }

    /// <summary>
    /// Event info for GetStringLiterals
    /// </summary>
    public class PluginGetStringLiteralsEventInfo : PluginEventInfo
    {
        /// <summary>
        /// All of the fetched string literals to be returned
        /// </summary>
        public List<string> StringLiterals { get; set; } = new List<string>();
    }

    /// <summary>
    /// Event info for PreProcessImage
    /// </summary>
    public class PluginPreProcessImageEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PostProcessImage
    /// </summary>
    public class PluginPostProcessImageEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PreProcessBinary
    /// </summary>
    public class PluginPreProcessBinaryEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PostProcessBinary
    /// </summary>
    public class PluginPostProcessBinaryEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PostProcessPackage
    /// </summary>
    public class PluginPostProcessPackageEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PostProcessTypeModel
    /// </summary>
    public class PluginPostProcessTypeModelEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for LoadPipelineEnding
    /// </summary>
    public class PluginLoadPipelineEndingEventInfo : PluginEventInfo { }

    /// <summary>
    /// Event info for PostProcessAppModel
    /// </summary>
    public class PluginPostProcessAppModelEventInfo : PluginEventInfo { }
}
