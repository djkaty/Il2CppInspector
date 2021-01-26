/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using McMaster.NETCore.Plugins;
using Il2CppInspector.PluginAPI;

// This is the ONLY line to update when the API version changes
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector
{
    public enum OptionBehaviour
    {
        None,
        IgnoreInvalid,
        NoValidation
    }

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

        // The current stack trace of the plugin
        internal Stack<string> StackTrace = new Stack<string>();

        // Get options as dictionary
        public Dictionary<string, object> GetOptions()
            => Plugin.Options.ToDictionary(o => o.Name, o => o.Value);

        // Set options to values with optional validation behaviour
        public void SetOptions(Dictionary<string, object> options, OptionBehaviour behaviour = OptionBehaviour.None) {
            foreach (var option in options)
                // Throw an exception on the first invalid option
                if (behaviour == OptionBehaviour.None)
                    this[option.Key] = option.Value;

                // Don't set invalid options but don't throw an exception either
                else if (behaviour == OptionBehaviour.IgnoreInvalid)
                    try {
                        this[option.Key] = option.Value;
                    } catch { }

                // Force set options with no validation
                else if (behaviour == OptionBehaviour.NoValidation) {
                    if (Plugin.Options.FirstOrDefault(o => o.Name == option.Key) is IPluginOption target) {
                        var validationCondition = target.If;
                        target.If = () => false;
                        target.Value = option.Value;
                        target.If = validationCondition;
                    }
                }
        }
    }

    // Event arguments for error handler
    public class PluginErrorEventArgs : EventArgs
    {
        // The plugin that the event originated from
        public IPlugin Plugin { get; set; }

        // The exception thrown
        public Exception Exception { get; set; }

        // The name of the method that was being executed
        public string Operation { get; set; }
    }

    // Event arguments for option handler
    public class PluginOptionErrorEventArgs : PluginErrorEventArgs
    {
        // The option causing the problem
        public IPluginOption Option { get; set; }
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
        // Global enable/disable flag for entire plugin system
        // If set to false, all plugins will be unloaded
        // Disable this if you want to create standalone apps using the API but without plugins
        private static bool _enabled = true;
        public static bool Enabled {
            get => _enabled;
            set {
                _enabled = value;
                Reload();
            }
        }

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
        public static void Reload(string pluginPath = null, bool reset = true, bool coreOnly = false) {
            // Update plugin folder if requested, otherwise use current setting
            pluginFolder = pluginPath ?? pluginFolder;

            if (reset)
                AsInstance.ManagedPlugins.Clear();

            // Do nothing if plugin system disabled except unload every plugin
            if (!Enabled)
                return;

            // Don't allow the user to start the application if there's no plugins folder
            if (!Directory.Exists(pluginFolder)) {
                throw new DirectoryNotFoundException(
                    "Plugins folder not found. Please ensure you have installed the latest set of plugins before starting. "
                  + "The plugins folder should be placed in the same directory as Il2CppInspector. "
                  + "Use get-plugins.ps1 or get-plugins.sh to update your plugins. For more information, see the Il2CppInspector README.md file.");
            }

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

                // Construct disabled plugin as a placeholder if loading fails (mainly for the GUI)
                var disabledPlugin = new ManagedPlugin {
                    Plugin = null,
                    Available = false,
                    Enabled = false
                };

                // Load plugin
                try {
                    var asm = loader.LoadDefaultAssembly();

                    // Determine plugin version and instantiate as appropriate
                    foreach (var type in asm.GetTypes()) {
                        // Current version
                        if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsAbstract) {

                            var isCorePlugin = typeof(ICorePlugin).IsAssignableFrom(type);

                            if (coreOnly && !isCorePlugin)
                                continue;

                            var plugin = (IPlugin) Activator.CreateInstance(type);

                            // Don't allow multiple identical plugins to load
                            if (AsInstance.ManagedPlugins.Any(p => p.Plugin.Id == plugin.Id))
                                throw new Exception($"Multiple copies of {plugin.Name} were found. Please ensure the plugins folder only contains one copy of each plugin.");

                            // Enable internal plugins by default
                            AsInstance.ManagedPlugins.Add(new ManagedPlugin { Plugin = plugin, Available = true, Enabled = isCorePlugin });
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

                    AsInstance.ManagedPlugins.Add(disabledPlugin);

                    // Determine error
                    switch (ex.LoaderExceptions[0]) {

                        // Type could not be found
                        case TypeLoadException failedType:
                            if (failedType.TypeName.StartsWith("Il2CppInspector.PluginAPI.")) {

                                // Requires newer plugin API version
                                disabledPlugin.Plugin = new InvalidPlugin { Name = name, Description = "This plugin requires a newer version of Il2CppInspector" };
                                Console.Error.WriteLine($"Error loading plugin {disabledPlugin.Plugin.Name}: {disabledPlugin.Plugin.Description}");
                            } else {

                                // Missing dependencies or some inherited interfaces not implemented
                                disabledPlugin.Plugin = new InvalidPlugin { Name = name, Description = "This plugin has dependencies that could not be found or may require a newer version of Il2CppInspector. Check that all required DLLs are present in the plugins folder." };
                                Console.Error.WriteLine($"Error loading plugin {disabledPlugin.Plugin.Name}: {disabledPlugin.Plugin.Description}");
                            }
                            break;

                        // Assembly could not be found
                        case FileNotFoundException failedFile:
                            disabledPlugin.Plugin = new InvalidPlugin { Name = name, Description = $"This plugin needs {failedFile.FileName} but the file could not be found" };
                            Console.Error.WriteLine($"Error loading plugin {disabledPlugin.Plugin.Name}: {disabledPlugin.Plugin.Description}");
                            break;

                        // Some other type loading error
                        default:
                            throw new InvalidOperationException($"Fatal error loading plugin {name}: {ex.LoaderExceptions[0].GetType()} - {ex.LoaderExceptions[0].Message}");
                    }
                }

                // Some field not implemented in class
                catch (TargetInvocationException ex) when (ex.InnerException is MissingFieldException fEx) {
                    var name = Path.GetFileName(dll);

                    disabledPlugin.Plugin = new InvalidPlugin { Name = name, Description = fEx.Message };
                    Console.Error.WriteLine($"Error loading plugin {disabledPlugin.Plugin.Name}: {disabledPlugin.Plugin.Description} - this plugin may require a newer version of Il2CppInspector");
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

        // Reset a plugin to its default "factory" state
        public static IPlugin Reset(IPlugin plugin) {
            var managedPlugin = AsInstance.ManagedPlugins.Single(p => p.Plugin == plugin);

            var replacement = (IPlugin) Activator.CreateInstance(plugin.GetType());
            managedPlugin.Plugin = replacement;
            return replacement;
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

        // Validate all options for enabled plugins
        public static PluginOptionsChangedEventInfo ValidateAllOptions() {
            // Enforce this by causing each option's setter to run
            var eventInfo = new PluginOptionsChangedEventInfo();

            foreach (var plugin in EnabledPlugins)
                if (plugin.Options != null)
                    foreach (var option in plugin.Options)
                        try {
                            option.Value = option.Value;
                        }
                        catch (Exception ex) {
                            eventInfo.Error = new PluginOptionErrorEventArgs { Plugin = plugin, Exception = ex, Option = option, Operation = "options update" };
                            ErrorHandler?.Invoke(AsInstance, eventInfo);
                            break;
                        }
            return eventInfo;
        }

        // Try to cast each enabled plugin to a specific interface type, and for those supporting the interface, execute the supplied delegate
        // Errors will be forwarded to the error handler
        internal static E Try<I, E>(Action<I, E> action, [CallerMemberName] string hookName = null) where E : PluginEventInfo, new()
        {
            var eventInfo = new E();
            var enabledPlugins = AsInstance.ManagedPlugins.Where(p => p.Enabled);

            foreach (var plugin in enabledPlugins)
                if (plugin.Plugin is I p)
                    try {
                        // Silently disallow recursion unless [Reentrant] is set on the method
                        if (plugin.StackTrace.Contains(hookName)) {
                            var allowRecursion = p.GetType().GetMethod(hookName).GetCustomAttribute(typeof(ReentrantAttribute)) != null;
                            if (!allowRecursion)
                                continue;
                        }

                        plugin.StackTrace.Push(hookName);
                        action(p, eventInfo);
                        plugin.StackTrace.Pop();

                        if (eventInfo.FullyProcessed)
                            break;
                    }
                    catch (Exception ex) {
                        // Disable failing plugin
                        plugin.Enabled = false;

                        // Clear stack trace
                        plugin.StackTrace.Clear();

                        // Forward error to error handler
                        eventInfo.Error = new PluginErrorEventArgs { Plugin = plugin.Plugin, Exception = ex, Operation = hookName };
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
