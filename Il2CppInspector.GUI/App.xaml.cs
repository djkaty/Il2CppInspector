// Copyright (c) 2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Il2CppInspector;
using Il2CppInspector.Model;
using Il2CppInspector.Reflection;
using Inspector = Il2CppInspector.Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        private Metadata metadata;

        // True if we extracted from an APK, IPA, zip file etc.
        private bool isExtractedFromPackage;
        public bool IsExtractedFromPackage {
            get => isExtractedFromPackage;
            set {
                if (value == isExtractedFromPackage) return;
                isExtractedFromPackage = value;
                OnPropertyChanged();
            }
        }

        public LoadOptions LoadOptions { get; private set; } = null;

        public List<AppModel> AppModels { get; } = new List<AppModel>();

        public Exception LastException { get; private set; }

        // Event to indicate current work status
        public event EventHandler<string> OnStatusUpdate;

        private void StatusUpdate(object sender, string status) => OnStatusUpdate?.Invoke(sender, status);

        public void ResetLoadOptions() {
            LoadOptions = new LoadOptions {
                ImageBase = 0ul
            };
        }

        // Attempt to load an IL2CPP application package (APK or IPA)
        public async Task<bool> LoadPackageAsync(IEnumerable<string> packageFiles) {
            IsExtractedFromPackage = false;

            try {
                OnStatusUpdate?.Invoke(this, "Extracting package");

                var streams = await Task.Run(() => Inspector.GetStreamsFromPackage(packageFiles));
                if (streams == null)
                    throw new InvalidOperationException("The supplied package is not an APK or IPA file, or does not contain a complete IL2CPP application");

                IsExtractedFromPackage = await LoadMetadataAsync(streams.Value.Metadata) && await LoadBinaryAsync(streams.Value.Binary);
                return IsExtractedFromPackage;
            }
            catch (Exception ex) {
                LastException = ex;
                return false;
            }
        }

        // Attempt to load an IL2CPP metadata file
        public async Task<bool> LoadMetadataAsync(string metadataFile) {
            IsExtractedFromPackage = false;
            var stream = new MemoryStream(await File.ReadAllBytesAsync(metadataFile));
            return await LoadMetadataAsync(stream);
        }

        public Task<bool> LoadMetadataAsync(MemoryStream metadataStream) =>
            Task.Run(() => {
                try {
                    OnStatusUpdate?.Invoke(this, "Processing metadata");

                    metadata = new Metadata(metadataStream, StatusUpdate);
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });

        // Attempt to load an IL2CPP binary file
        public async Task<bool> LoadBinaryAsync(string binaryFile) {
            var stream = new MemoryStream(await File.ReadAllBytesAsync(binaryFile));
            return await LoadBinaryAsync(stream);
        }

        public Task<bool> LoadBinaryAsync(Stream binaryStream) =>
            Task.Run(() => {
                try {
                    OnStatusUpdate?.Invoke(this, "Processing binary");

                    // This may throw other exceptions from the individual loaders as well
                    IFileFormatReader stream = FileFormatReader.Load(binaryStream, LoadOptions, StatusUpdate);
                    if (stream == null) {
                        throw new InvalidOperationException("Could not determine the binary file format");
                    }
                    if (stream.NumImages == 0) {
                        throw new InvalidOperationException("Could not find any binary images in the file");
                    }

                    // Multi-image binaries may contain more than one Il2Cpp image
                    AppModels.Clear();
                    foreach (var image in stream.Images) {
                        OnStatusUpdate?.Invoke(this, $"Analyzing IL2CPP data for {image.Format}/{image.Arch} image");

                        // Architecture-agnostic load attempt
                        try {
                            // If we can't load the IL2CPP data here, it's probably packed or obfuscated; ignore it
                            if (Il2CppBinary.Load(image, metadata, StatusUpdate) is Il2CppBinary binary) {
                                var inspector = new Inspector(binary, metadata);

                                // Build type model
                                OnStatusUpdate?.Invoke(this, $"Building .NET type model for {image.Format}/{image.Arch} image");
                                var typeModel = new TypeModel(inspector);

                                // Initialize (but don't build) application model
                                // We will build the model after the user confirms the Unity version and target compiler
                                AppModels.Add(new AppModel(typeModel, makeDefaultBuild: false));
                            }
                        }
                        // Unsupported architecture; ignore it
                        catch (NotImplementedException) { }
                    }
                    if (!AppModels.Any()) {
                        throw new InvalidOperationException("Could not auto-detect any IL2CPP binary images in the file. This may mean the binary file is packed, encrypted or obfuscated, that the file is not an IL2CPP image or that Il2CppInspector was not able to automatically find the required data. Please check the binary file in a disassembler to ensure that it is an unencrypted IL2CPP binary before submitting a bug report!");
                    }
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });

        // Property change notifier for IsExtractedFromPackage binding
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
