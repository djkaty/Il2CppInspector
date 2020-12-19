/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

// This is the ONLY line to update when the API version changes
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector
{
    // Hooks we provide to plugins which can choose whether or not to provide implementations
    internal static class PluginHooks
    {
        public static void PostProcessMetadata(Metadata metadata) => PluginManager.Try<IPostProcessMetadata>(p => p.PostProcessMetadata(metadata));
    }
}
