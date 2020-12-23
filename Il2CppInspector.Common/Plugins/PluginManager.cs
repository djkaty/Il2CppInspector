/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using McMaster.NETCore.Plugins;

// This is the ONLY line to update when the API version changes
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector
{
    // Internal settings for a plugin
    public class ManagedPlugin
    {
        // The plugin itself
        public IPlugin Plugin { get; set; }

        // The plugin is enabled for execution
        public bool Enabled { get; set; }

        // The plugin is valid and compatible with this version of the host
        public bool Available { get; set; }

        // Programmatic access to options
        public object this[string s] {
            get => Plugin.Options.Single(o => o.Name == s).Value;
            set => Plugin.Options.Single(o => o.Name == s).Value = value;
        }
    }

    // Event arguments for error handler
    public class PluginErrorEventArgs : EventArgs
    {
        // The plugin that the event originated from
        public IPlugin Plugin { get; set; }

        // The exception thrown
        public Exception Exception { get; set; }

        // The name of the operation that was being performed
        public string Operation { get; set; }
    }

    // Event arguments for the status handler
    public class PluginStatusEventArgs : EventArgs
    {
        // The plugin that the event originated from
        public IPlugin Plugin { get; set; }

        // The status update text
        public string Text { get; set; }
    }

    // Singleton for managing external plugins
    public partial class PluginManager
    {
        // All of the detected plugins, including invalid/incompatible/non-loaded plugins
        public ObservableCollection<ManagedPlugin> ManagedPlugins { get; } = new ObservableCollection<ManagedPlugin>();

        // All of the plugins that are loaded and available for use
        public static IEnumerable<IPlugin> AvailablePlugins => AsInstance.ManagedPlugins.Where(p => p.Available).Select(p => p.Plugin);

        // All of the plugins that are currently enabled and will be called into
        public static IEnumerable<IPlugin> EnabledPlugins => AsInstance.ManagedPlugins.Where(p => p.Enabled).Select(p => p.Plugin);

        // All of the plugins that are loaded and available for use, indexed by plugin ID
        public static Dictionary<string, ManagedPlugin> Plugins
            => AsInstance.ManagedPlugins.Where(p => p.Available).ToDictionary(p => p.Plugin.Id, p => p);

        // The relative path from the executable that we'll search for plugins
        private static string pluginFolder = Path.GetFullPath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + Path.DirectorySeparatorChar + "plugins");

        // A placeholder plugin to be used when the real plugin cannot be loaded for some reason
        private class InvalidPlugin : IPlugin
        {
            public string Id => "_invalid_";

            public string Name { get; set; }

            public string Author => "unknown";

            public string Description { get; set; }

            public string Version => "not loaded";

            public List<IPluginOption> Options => null;
        };

        // Singleton pattern
        private static PluginManager _singleton;
        public static PluginManager AsInstance {
            get {
                if (_singleton == null) {
                    _singleton = new PluginManager();
                    Reload();
                }
                return _singleton;
            }
        }

        // We don't call Reload() in the constructor to avoid infinite recursion
        private PluginManager() { }

        // Error handler called when a plugin throws an exception
        // This should be hooked by the client consuming the Il2CppInspector class library
        // If not used, all exceptions are suppressed (which is probably really bad)
        public static event EventHandler<PluginEventInfo> ErrorHandler;

        // Handler called when a plugin reports a status update
        // If not used, all status updates are suppressed
        public static event EventHandler<PluginStatusEventArgs> StatusHandler;

        // Force plugins to load if they haven't already
        public static PluginManager EnsureInit() => AsInstance;

        // Find and load all available plugins from disk
        public static void Reload(string pluginPath = null) {
            // Update plugin folder if requested, otherwise use current setting
            pluginFolder = pluginPath ?? pluginFolder;

            AsInstance.ManagedPlugins.Clear();

            // Don't do anything if there's no plugins folder
            if (!Directory.Exists(pluginFolder))
                return;

            // Get every DLL
            // NOTE: Every plugin should be in its own folder together with its dependencies
            var dlls = Directory.GetFiles(pluginFolder, "*.dll", SearchOption.AllDirectories);

            foreach (var dll in dlls) {
                // All plugin interfaces we allow for this version of Il2CppInspector
                // Add new versions to allow backwards compatibility
                var loader = PluginLoader.CreateFromAssemblyFile(dll,
                   sharedTypes: new[] {
                        typeof(PluginAPI.V100.IPlugin),
                        //typeof(PluginAPI.V101.IPlugin)
                   });

                // Load plugin
                try {
                    var asm = loader.LoadDefaultAssembly();

                    // Determine plugin version and instantiate as appropriate
                    foreach (var type in asm.GetTypes()) {
                        // Current version
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract) {
                            var plugin = (IPlugin) Activator.CreateInstance(type);
                            AsInstance.ManagedPlugins.Add(new ManagedPlugin { Plugin = plugin, Available = true, Enabled = false });
                        }

                        // Add older versions here with adapters
                        /*
                        // V100
                        else if (typeof(PluginAPI.V100.IPlugin).IsAssignableFrom(type) && !type.IsAbstract) {
                            var plugin = (PluginAPI.V100.IPlugin) Activator.CreateInstance(type);
                            var adapter = new PluginAPI.V101.Adapter(plugin);
                            Plugins.Add(new Plugin { Interface = adapter, Available = true, Enabled = false });
                        }*/
                    }
                }

                // Problem finding all the types required to load the plugin
                catch (ReflectionTypeLoadException ex) {
                    var name = Path.GetFileName(dll);

                    // Construct disabled plugin
                    var plugin = new ManagedPlugin {
                        Plugin = null,
                        Available = false,
                        Enabled = false
                    };
                    AsInstance.ManagedPlugins.Add(plugin);

                    // Determine error
                    switch (ex.LoaderExceptions[0]) {

                        // Type could not be found
                        case TypeLoadException failedType:
                            if (failedType.TypeName.StartsWith("Il2CppInspector.PluginAPI.")) {

                                // Requires newer plugin API version
                                plugin.Plugin = new InvalidPlugin { Name = name, Description = "This plugin requires a newer version of Il2CppInspector" };
                                Console.Error.WriteLine($"Error loading plugin {plugin.Plugin.Name}: {plugin.Plugin.Description}");
                            } else {

                                // Missing dependencies
                                plugin.Plugin = new InvalidPlugin { Name = name, Description = "This plugin has dependencies that could not be found. Check that all required DLLs are present in the plugins folder." };
                                Console.Error.WriteLine($"Error loading plugin {plugin.Plugin.Name}: {plugin.Plugin.Description}");
                            }
                            break;

                        // Assembly could not be found
                        case FileNotFoundException failedFile:
                            plugin.Plugin = new InvalidPlugin { Name = name, Description = $"This plugin needs {failedFile.FileName} but the file could not be found" };
                            Console.Error.WriteLine($"Error loading plugin {plugin.Plugin.Name}: {plugin.Plugin.Description}");
                            break;

                        // Some other type loading error
                        default:
                            throw new InvalidOperationException($"Fatal error loading plugin {name}: {ex.LoaderExceptions[0].GetType()} - {ex.LoaderExceptions[0].Message}");
                    }
                }

                // Ignore unmanaged DLLs
                catch (BadImageFormatException) { }

                // Some other load error (probably generated by the plugin itself)
                catch (Exception ex) {
                    var name = Path.GetFileName(dll);

                    throw new InvalidOperationException($"Fatal error loading plugin {name}: {ex.GetType()} - {ex.Message}", ex);
                }
            }
        }

        // Commit options change for the specified plugin
        public static PluginOptionsChangedEventInfo OptionsChanged(IPlugin plugin) {
            var eventInfo = new PluginOptionsChangedEventInfo();

            try {
                plugin.OptionsChanged(eventInfo);
            }
            catch (Exception ex) {
                eventInfo.Error = new PluginErrorEventArgs { Plugin = plugin, Exception = ex, Operation = "options update" };
                ErrorHandler?.Invoke(AsInstance, eventInfo);
            }

            return eventInfo;
        }

        // Try to cast each enabled plugin to a specific interface type, and for those supporting the interface, execute the supplied delegate
        // Errors will be forwarded to the error handler
        internal static E Try<I, E>(Action<I, E> action) where E : PluginEventInfo, new()
        {
            var eventInfo = new E();

            foreach (var plugin in EnabledPlugins)
                if (plugin is I p)
                    try {
                        action(p, eventInfo);

                        if (eventInfo.IsHandled)
                            break;
                    }
                    catch (Exception ex) {
                        eventInfo.Error = new PluginErrorEventArgs { Plugin = plugin, Exception = ex, Operation = typeof(I).Name };
                        ErrorHandler?.Invoke(AsInstance, eventInfo);
                    }

            return eventInfo;
        }

        // Process an incoming status update
        internal static void StatusUpdate(IPlugin plugin, string text) {
            StatusHandler?.Invoke(AsInstance, new PluginStatusEventArgs { Plugin = plugin, Text = text });
        }
    }
}
