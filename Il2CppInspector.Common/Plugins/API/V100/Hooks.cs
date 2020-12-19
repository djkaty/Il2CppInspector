/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Process global-metadata.dat after it has been loaded into a Metadata object
    /// </summary>
    public interface IPostProcessMetadata
    {
        void PostProcessMetadata(Metadata metadata);
    }
}
