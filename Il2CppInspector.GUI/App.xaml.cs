using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Il2CppInspector;
using Inspector = Il2CppInspector.Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Metadata metadata;

        public List<Inspector> Il2CppImages { get; } = new List<Inspector>();

        public Exception LastException { get; private set; }

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
                    IFileFormatReader stream = FileFormatReader.Load(binaryFile);
                    if (stream == null) {
                        throw new InvalidOperationException("Could not determine the binary file format");
                    }
                    if (!stream.Images.Any()) {
                        throw new InvalidOperationException("Could not find any binary images in the file");
                    }

                    // Multi-image binaries may contain more than one Il2Cpp image
                    Il2CppImages.Clear();
                    foreach (var image in stream.Images) {
                        // Architecture-agnostic load attempt
                        try {
                            // If we can't load the IL2CPP data here, it's probably packed or obfuscated; ignore it
                            if (Il2CppBinary.Load(image, metadata.Version) is Il2CppBinary binary) {
                                Il2CppImages.Add(new Inspector(binary, metadata));
                            }
                        }
                        // Unsupported architecture; ignore it
                        catch (NotImplementedException) { }
                    }
                    if (!Il2CppImages.Any()) {
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
