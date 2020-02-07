using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Metadata CurrentMetadata { get; private set; }

        public Exception LastException { get; private set; }

        // Attempt to load an IL2CPP metadata file
        public Task<bool> LoadMetadataAsync(string metadataFile) =>
            Task.Run(() => {
                try {
                    CurrentMetadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });
    }
}
