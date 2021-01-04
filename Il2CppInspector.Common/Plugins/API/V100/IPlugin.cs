/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System.Collections.Generic;

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Core interface that all plugins must implement
    /// </summary>
    public partial interface IPlugin
    {
        /// <summary>
        /// Plugin name for CLI and unique ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Human-readable plugin name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Nickname of the plugiin author
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Human-readable version string for the plugin
        /// Always use lexical order: version string may be used for updates
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Description of the plugin
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Plugin options with their current values (or default values on startup where applicable)
        /// </summary>
        public List<IPluginOption> Options { get; }

        /// <summary>
        /// Executes when the plugin's options are updated by the user
        /// Not called on first load (with the default, possibly incomplete options provided by the plugin author)
        /// Do not perform any long-running operations here
        /// Implementation is optional. The default is to do nothing
        /// </summary>
        void OptionsChanged(PluginOptionsChangedEventInfo e) { }
   }
}
