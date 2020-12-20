/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Il2CppInspector.PluginAPI.V100
{
    public interface IPluginEventInfo
    {
        public bool IsHandled { get; set; }
        public bool IsInvalid { get; set; }
        public bool IsDataModified { get; set; }
        public bool IsStreamModified { get; set; }
        public PluginErrorEventArgs Error { get; set; }
    }

    /// <summary>
    /// Object which allows plugins to report on what has happened during a call
    /// Changes made to this object propagate to the next plugin in the call chain until IsHandled is set to true
    /// </summary>
    public class PluginEventInfo<T> : IPluginEventInfo where T : new()
    {
        /// <summary>
        /// A plugin should set this if it has processed the supplied data in such a way that no further processing is required by other plugins
        /// Generally, this will prevent other plugins from processing the data
        /// Note that this should be set even if the processed data was invalid (<seealso cref="IsInvalid"/>)
        /// If this is not set, the same event will be raised on the next available plugin
        /// Note that you can still do processing but set IsHandled to false to allow additional processing from other plugins
        /// </summary>
        public bool IsHandled { get; set; } = false;

        /// <summary>
        /// A plugin should set this when IsHandled = true but the data it processed was invalid, for example if the processing gave an unexpected result
        /// </summary>
        public bool IsInvalid { get; set; } = false;

        /// <summary>
        /// A plugin should set this when it has directly modified the provided data structure (object)
        /// This can be set even if IsHandled = false to indicate that changes have been made but more plugins can still be called
        /// Should be set to false if you have only queried (performed reads) on the data without changing it
        /// </summary>
        public bool IsDataModified { get; set; } = false;

        /// <summary>
        /// A plugin should set this when it has directly modified the supplied or underlying stream for the metadata or binary
        /// This can be set even if IsHandled = false to indicate that changes have been made but more plugins can still be called
        /// Should be set to false if you have only queried (performed reads) on the stream without changing it
        /// </summary>
        public bool IsStreamModified { get; set; } = false;

        /// <summary>
        /// Event-specific additional options and controls. See the documentation for each event for more details.
        /// </summary>
        public T AdditionalData { get; } = new T();

        /// <summary>
        /// This wiil be set automatically by Il2CppInspector to the last exception thrown by a plugin for the current event
        /// </summary>
        public PluginErrorEventArgs Error { get; set; } = null;
    }

    /// <summary>
    /// Generic event info with no additional paramters
    /// </summary>
    public class PluginEventInfo : PluginEventInfo<object> { }

    /// <summary>
    /// Event info for PreProcessMetadata
    /// </summary>
    public class PluginPreProcessMetadataEventInfo : PluginEventInfo<PluginPreProcessMetadataEventData> { }

    /// <summary>
    /// Event info for PostProcessMetadata
    /// </summary>
    public class PluginPostProcessMetadataEventInfo : PluginEventInfo { }

    /// <summary>
    /// Additional data for PreProcessMetadata
    /// </summary>
    public class PluginPreProcessMetadataEventData
    {
        /// <summary>
        /// Set to true to disable some validation checks by Il2CppInspector that the metadata is valid
        /// </summary>
        public bool SkipValidation { get; set; }
    }
}
