/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Il2CppInspector;
using Il2CppInspector.Model;
using Il2CppInspector.PluginAPI.V100;
using Il2CppInspector.Reflection;
using Inspector = Il2CppInspector.Il2CppInspector;

namespace Il2CppInspectorGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        // Catch unhandled exceptions for debugging startup failures and plugins
        public App() : base() {
            var np = Environment.NewLine + Environment.NewLine;

            Dispatcher.UnhandledException += (s, e) => {
                MessageBox.Show(e.Exception.GetType() + ": " + e.Exception.Message
                                + np + e.Exception.StackTrace
                                + np + "More details may follow in subsequent error boxes",
                                "Il2CppInspector encountered a fatal error");

                if (e.Exception is FileNotFoundException fe)
                    MessageBox.Show("Missing file: " + fe.FileName, "Additional error information");

                var ex = e.Exception;
                while (ex.InnerException != null) {
                    MessageBox.Show(ex.GetType() + ": " + ex.Message + np
                        + (ex is XamlParseException xpe ? $"BaseUri: {xpe.BaseUri}, LineNumber: {xpe.LineNumber}, LinePosition: {xpe.LinePosition}, NameContext: {xpe.NameContext}" + np : "")
                        + (ex is TypeInitializationException tie ? "Type which failed to initialize: " + tie.TypeName + np : "")
                        + ex.StackTrace, "Additional error information");
                    ex = ex.InnerException;
                }
            };

            // Load plugins
            PluginManager.EnsureInit();
        }

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

        public LoadOptions LoadOptions { get; private set; }

        public List<AppModel> AppModels { get; } = new List<AppModel>();

        public Exception LastException { get; private set; }

        // Event to indicate current work status
        public event EventHandler<string> OnStatusUpdate;

        private void StatusUpdate(object sender, string status) => OnStatusUpdate?.Invoke(sender, status);

        // Initialization entry point
        protected override void OnStartup(StartupEventArgs e) {
            // Set contents of load options window
            ResetLoadOptions();

            // Set handlers for plugin manager
            PluginManager.ErrorHandler += (s, e) => {
                if (e is PluginOptionsChangedEventInfo)
                    MessageBox.Show("Could not update plugin options. " + e.Error.Exception.Message, "Plugin error");
                else
                    MessageBox.Show($"The plugin {e.Error.Plugin.Name} encountered an error while executing {e.Error.Operation}: {e.Error.Exception.Message}."
                                + Environment.NewLine + Environment.NewLine + "Plugin has been disabled.", "Plugin error");
            };

            PluginManager.StatusHandler += (s, e) => StatusUpdate(e.Plugin, "[" + e.Plugin.Name + "]\n" + e.Text);
        }

        public void ResetLoadOptions() {
            LoadOptions = new LoadOptions {
                ImageBase = 0xffffffff_ffffffff,
                BinaryFilePath = null
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
                    metadata = Metadata.FromStream(metadataStream, StatusUpdate);
                    return true;
                }
                catch (Exception ex) {
                    LastException = ex;
                    return false;
                }
            });

        // Attempt to load an IL2CPP binary file
        public async Task<bool> LoadBinaryAsync(string binaryFile) {
            // For loaders which require the file path to find additional files
            LoadOptions.BinaryFilePath = binaryFile;

            var stream = new MemoryStream(await File.ReadAllBytesAsync(binaryFile));
            return await LoadBinaryAsync(stream);
        }

        public Task<bool> LoadBinaryAsync(Stream binaryStream) =>
            Task.Run(() => {
                try {
                    OnStatusUpdate?.Invoke(this, "Processing binary");

                    // This may throw other exceptions from the individual loaders as well
                    IFileFormatStream stream = FileFormatStream.Load(binaryStream, LoadOptions, StatusUpdate);
                    if (stream == null) {
                        throw new InvalidOperationException("Could not determine the binary file format");
                    }
                    if (stream.NumImages == 0) {
                        throw new InvalidOperationException("Could not find any binary images in the file");
                    }

                    // Get an Il2CppInspector for each image
                    var inspectors = Inspector.LoadFromStream(stream, metadata, StatusUpdate);

                    AppModels.Clear();

                    foreach (var inspector in inspectors) {
                        // Build type model
                        OnStatusUpdate?.Invoke(this, $"Building .NET type model for {inspector.BinaryImage.Format}/{inspector.BinaryImage.Arch} image");
                        var typeModel = new TypeModel(inspector);

                        // Initialize (but don't build) application model
                        // We will build the model after the user confirms the Unity version and target compiler
                        AppModels.Add(new AppModel(typeModel, makeDefaultBuild: false));
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
