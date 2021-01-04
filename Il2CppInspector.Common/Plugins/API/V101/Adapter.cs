/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

/// This is an example for future use of how to enable backwards-compatibility
/// by allowing newer versions of Il2CppInspector to load plugins made with older API versions

using System.Collections.Generic;
/*
namespace Il2CppInspector.PluginAPI.V101
{
    /// <summary>
    /// Converts a V100 plugin to a V101 plugin
    /// </summary>
    public class Adapter : IPlugin
    {
        private V100.IPlugin plugin;
        public Adapter(V100.IPlugin plugin) => this.plugin = plugin;

        // Fill in missing interface contract
        public string Id => plugin.Id;
        public string Name => plugin.Name;
        public string Version => plugin.Version;
        public string Description => plugin.Description;
        public List<V100.IPluginOption> Options => plugin.Options;
    }
}
*/