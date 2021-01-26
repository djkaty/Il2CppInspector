/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Il2CppInspector;
using Il2CppInspector.GUI;
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
        // Converter for IPlugin for System.Text.Json
        private class PluginConverter : JsonConverter<IPlugin>
        {
            public override IPlugin Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                return (IPlugin) JsonSerializer.Deserialize(ref reader, typeof(PluginState), options);
            }

            public override void Write(Utf8JsonWriter writer, IPlugin value, JsonSerializerOptions options) {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }

        private class PluginOptionConverter : JsonConverter<IPluginOption>
        {
            public override IPluginOption Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
                return (IPluginOption) JsonSerializer.Deserialize(ref reader, typeof(PluginOptionState), options);
            }

            public override void Write(Utf8JsonWriter writer, IPluginOption value, JsonSerializerOptions options) {
                JsonSerializer.Serialize(writer, new PluginOptionState { Name = value.Name, Value = value.Value }, typeof(PluginOptionState), options);
            }
        }

        // Save state for plugins
        private class PluginState : IPlugin
        {
            public string Id { get; set; }
            [JsonIgnore]
            public string Name { get; set; }
            [JsonIgnore]
            public string Author { get; set; }
            [JsonIgnore]
            public string Description { get; set; }
            [JsonIgnore]
            public string Version { get; set; }
            public List<IPluginOption> Options { get; set; }
        };

        // Save state for plugin options
        private class PluginOptionState : IPluginOption
        {
            public string Name { get; set; }
            [JsonIgnore]
            public string Description { get; set; }
            [JsonIgnore]
            public bool Required { get; set; }
            public object Value { get; set; }
            [JsonIgnore]
            public Func<bool> If { get; set; }
            public void SetFromString(string value) { }
        }

        // Application startup
        public App() : base() {
            // Catch unhandled exceptions for debugging startup failures and plugins
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

            // Load options
            LoadOptions();
        }

        // Load options from user config
        internal void LoadOptions() {
            // Migrate settings from previous version if necessary
            if (User.Default.UpgradeRequired) {
                User.Default.Upgrade();
                User.Default.UpgradeRequired = false;
                User.Default.Save();
            }

            // Load plugin state (enabled / execution order)
            var savedPluginState = Array.Empty<ManagedPlugin>();
            try {
                savedPluginState = JsonSerializer.Deserialize<ManagedPlugin[]>(User.Default.PluginsState,
                    new JsonSerializerOptions { Converters = { new PluginConverter(), new PluginOptionConverter() } });
            }

            // Not set or invalid - just create a new set
            catch (JsonException) { }
            catch (NotSupportedException) { }

            // Load plugins if they aren't already
            try {
                PluginManager.EnsureInit();
            } catch (Exception ex) when (ex is InvalidOperationException || ex is DirectoryNotFoundException) {
                MessageBox.Show(ex.Message, "Fatal error loading plugins");
                Environment.Exit(1);
            }

            // Arrange plugins
            var loadedPlugins = PluginManager.AsInstance.ManagedPlugins;
            foreach (var savedState in savedPluginState.Reverse()) {
                if (loadedPlugins.FirstOrDefault(p => p.Plugin.Id == savedState.Plugin.Id) is ManagedPlugin managedPlugin) {

                    // Re-order to match saved order
                    loadedPlugins.Remove(managedPlugin);
                    loadedPlugins.Insert(0, managedPlugin);

                    // Enable/disable to match saved state
                    managedPlugin.Enabled = savedState.Enabled;

                    // Set options
                    // TODO: Use IPluginOption.SetFromString() instead

                    if (savedState.Plugin.Options != null) {
                        var options = new Dictionary<string, object>();

                        foreach (var savedOption in savedState.Plugin.Options)
                            if (managedPlugin.Plugin.Options.FirstOrDefault(o => o.Name == savedOption.Name) is IPluginOption option) {
                                if (savedOption.Value == null)
                                    options.Add(option.Name, null);
                                else
                                    options.Add(option.Name, (savedOption.Value, ((JsonElement) savedOption.Value).ValueKind) switch {
                                        (var v, JsonValueKind.String) => v.ToString(),
                                        (var v, JsonValueKind.Number) => option.Value.GetType().IsEnum?
                                              Enum.TryParse(option.Value.GetType(), v.ToString(), out object e)? e : throw new InvalidCastException("Enum value removed")
                                            : Convert.ChangeType(v.ToString(), option.Value.GetType()),
                                        (var v, JsonValueKind.True) => true,
                                        (var v, JsonValueKind.False) => false,
                                        _ => throw new ArgumentException("Unsupported JSON type")
                                    });
                            }

                        // An invalid cast will occur if a plugin author changes the type of one of its options
                        try {
                            managedPlugin.SetOptions(options, OptionBehaviour.NoValidation);
                        } catch (InvalidCastException) { }
                    }
                }
            }

            // Save options in case no save exists or previous save is invalid
            SaveOptions();
        }

        // Save options to user config
        internal void SaveOptions() {
            User.Default.PluginsState = JsonSerializer.Serialize(
                PluginManager.Plugins.Values.Cast<ManagedPlugin>().Where(p => p.Available).ToArray(),
                new JsonSerializerOptions { Converters = { new PluginConverter(), new PluginOptionConverter() } });
            User.Default.Save();
        }

        private Metadata metadata;

        // True if we extracted the current workload from an APK, IPA, zip file etc.
        private bool isExtractedFromPackage;
        public bool IsExtractedFromPackage {
            get => isExtractedFromPackage;
            set {
                if (value == isExtractedFromPackage) return;
                isExtractedFromPackage = value;
                OnPropertyChanged();
            }
        }

        // Load options for the current image
        public LoadOptions ImageLoadOptions { get; private set; }

        // Application models for the current image
        public List<AppModel> AppModels { get; } = new List<AppModel>();

        // The last exception thrown
        // Reading the exception clears it
        private Exception _lastException;
        public Exception LastException {
            get {
                var ex = _lastException;
                _lastException = null;
                return ex;
            }
            private set => _lastException = value;
        }

        // Event to indicate current work status
        public event EventHandler<string> OnStatusUpdate;

        private void StatusUpdate(object sender, string status) => OnStatusUpdate?.Invoke(sender, status);

        // Initialization entry point
        protected override void OnStartup(StartupEventArgs e) {
            // Set contents of load options window
            ResetLoadOptions();

            // Set handlers for plugin manager
            PluginManager.ErrorHandler += (s, e) => {
                if (e is PluginOptionsChangedEventInfo oe)
                    if (oe.Error is PluginOptionErrorEventArgs oea)
                        MessageBox.Show($"Plugin option '{oea.Option.Description}' for {e.Error.Plugin.Name} is invalid: {e.Error.Exception.Message}", "Plugin error");
                    else
                        MessageBox.Show($"One or more plugin options for {e.Error.Plugin.Name} are invalid: {e.Error.Exception.Message}", "Plugin error");
                else
                    MessageBox.Show($"The plugin {e.Error.Plugin.Name} encountered an error while executing {e.Error.Operation}: {e.Error.Exception.Message}."
                                + Environment.NewLine + Environment.NewLine + "Plugin has been disabled for this session.", "Plugin error");
            };

            PluginManager.StatusHandler += (s, e) => StatusUpdate(e.Plugin, "[" + e.Plugin.Name + "]\n" + e.Text);
        }

        // Reset image load options to their defaults
        public void ResetLoadOptions() {
            ImageLoadOptions = new LoadOptions();
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
                    // Don't start unless every enabled plugin's options are valid
                    if (PluginManager.ValidateAllOptions().Error != null)
                        return false;

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
            ImageLoadOptions.BinaryFilePath = binaryFile;

            var stream = new MemoryStream(await File.ReadAllBytesAsync(binaryFile));
            return await LoadBinaryAsync(stream);
        }

        public Task<bool> LoadBinaryAsync(Stream binaryStream) =>
            Task.Run(() => {
                try {
                    OnStatusUpdate?.Invoke(this, "Processing binary");

                    // This may throw other exceptions from the individual loaders as well
                    IFileFormatStream stream = FileFormatStream.Load(binaryStream, ImageLoadOptions, StatusUpdate);
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
