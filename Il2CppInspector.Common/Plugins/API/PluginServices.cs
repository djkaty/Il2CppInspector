/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector.PluginAPI
{
    /// <summary>
    /// Plugin-related services we provide to plugins that they can call upon
    /// </summary>
    public class PluginServices
    {
        // The plugin we are providing services to
        private IPlugin plugin;
        private PluginServices(IPlugin plugin) => this.plugin = plugin;

        /// <summary>
        /// Get plugin services for a specific plugin
        /// Designed to be used by plugins as a service factory
        /// </summary>
        /// <param name="plugin">The plugin to provide services to</param>
        /// <returns>A PluginServices object</returns>
        public static PluginServices For(IPlugin plugin) {
            return new PluginServices(plugin);
        }

        /// <summary>
        /// Provide visual status update text for long-running operations
        /// </summary>
        /// <param name="text">The text to report</param>
        public void StatusUpdate(string text) => PluginManager.StatusUpdate(plugin, text);

        /// <summary>
        /// An overload of StatusUpdate that can be cast to EventHandler<string>
        /// </summary>
        public void StatusUpdate(object sender, string text) => StatusUpdate(text);
    }
}
