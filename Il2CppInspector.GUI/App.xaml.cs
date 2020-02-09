using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Il2CppInspector;
using Il2CppInspector.Reflection;
using Inspector = Il2CppInspector.Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Metadata metadata;

        public List<Il2CppModel> Il2CppModels { get; } = new List<Il2CppModel>();

        public Exception LastException { get; private set; }

        // Event to indicate current work status
        public event EventHandler<string> OnStatusUpdate;

        private void StatusUpdate(object sender, string status) => OnStatusUpdate?.Invoke(sender, status);

        // Attempt to load an IL2CPP metadata file
        public Task<bool> LoadMetadataAsync(string metadataFile) =>
            Task.Run(() => {
                try {
                    metadata = new Metadata(new MemoryStream(File.ReadAllBytes(metadataFile)));
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });

        public Task<bool> LoadBinaryAsync(string binaryFile) =>
            Task.Run(() => {
                try {
                    // This may throw other exceptions from the individual loaders as well
                    IFileFormatReader stream = FileFormatReader.Load(binaryFile, StatusUpdate);
                    if (stream == null) {
                        throw new InvalidOperationException("Could not determine the binary file format");
                    }
                    if (!stream.Images.Any()) {
                        throw new InvalidOperationException("Could not find any binary images in the file");
                    }

                    // Multi-image binaries may contain more than one Il2Cpp image
                    Il2CppModels.Clear();
                    foreach (var image in stream.Images) {
                        OnStatusUpdate?.Invoke(this, "Analyzing IL2CPP data");

                        // Architecture-agnostic load attempt
                        try {
                            // If we can't load the IL2CPP data here, it's probably packed or obfuscated; ignore it
                            if (Il2CppBinary.Load(image, metadata.Version) is Il2CppBinary binary) {
                                var inspector = new Inspector(binary, metadata);

                                // Build type model
                                OnStatusUpdate?.Invoke(this, "Building type model");
                                Il2CppModels.Add(new Il2CppModel(inspector));
                            }
                        }
                        // Unsupported architecture; ignore it
                        catch (NotImplementedException) { }
                    }
                    if (!Il2CppModels.Any()) {
                        throw new InvalidOperationException("Could not auto-detect any IL2CPP binary images in the file. This may mean the binary file is packed, encrypted or obfuscated, that the file is not an IL2CPP image or that Il2CppInspector was not able to automatically find the required data. Please check the binary file in a disassembler to ensure that it is an unencrypted IL2CPP binary before submitting a bug report!");
                    }
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });
    }
}
