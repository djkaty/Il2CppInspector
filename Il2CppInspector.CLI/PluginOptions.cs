/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using CommandLine;
using CommandLine.Text;
using Il2CppInspector.PluginAPI.V100;

namespace Il2CppInspector.CLI
{
    // This ridiculous hack converts options from our plugin API to options classes that CommandLineParser can process and back again
    internal static class PluginOptions
    {
        // Create an auto-property
        private static PropertyBuilder CreateAutoProperty(TypeBuilder tb, string propertyName, Type propertyType) {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);

            return propertyBuilder;
        }

        // Create a CommandLineParser-friendly attributed class of options from a loaded plugin
        private static Type CreateOptionsFromPlugin(IPlugin plugin) {
            // Name of class to create
            var tn = plugin.Id + "Options";

            // Create class and default constructor
            var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            var mb = ab.DefineDynamicModule("MainModule");
            var pluginOptionClass = mb.DefineType(tn,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            pluginOptionClass.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            // Create VerbAttribute with plugin ID
            var verbCtorInfo = typeof(VerbAttribute).GetConstructor(new Type[] { typeof(string) });
            var verbHelpPropInfo = typeof(VerbAttribute).GetProperty("HelpText");
            var verbAttBuilder = new CustomAttributeBuilder(verbCtorInfo, new object[] { plugin.Id },
                                        new PropertyInfo[] { verbHelpPropInfo }, new object[] { plugin.Description });
            pluginOptionClass.SetCustomAttribute(verbAttBuilder);

            // Create auto-property for each option
            if (plugin.Options == null)
                return pluginOptionClass.CreateTypeInfo().AsType();

            foreach (var option in plugin.Options) {
                var optionType = option.GetType().GetProperty("Value").PropertyType;
                var optionValue = option.Value;

                // Enum types aren't supported by CommandLineParser in dynamic assemblies
                if (optionType.IsEnum) {
                    optionType = typeof(string);
                    optionValue = optionValue.ToString();
                }

                // We won't set the Required flag if there is a default option
                var optionEmpty = (optionType.IsValueType && optionValue == Activator.CreateInstance(optionType))
                                    || optionValue == null;

                // Hex numbers are strings that will be validated later
                if (option is IPluginOptionNumber n && n.Style == PluginOptionNumberStyle.Hex) {
                    optionType = typeof(string);
                    optionValue = string.Format("0x{0:x}", optionValue);
                }

                var pluginOptionProperty = CreateAutoProperty(pluginOptionClass, option.Name, optionType);

                ConstructorInfo optCtorInfo;

                // Single character
                if (option.Name.Length == 1)
                    optCtorInfo = typeof(OptionAttribute).GetConstructor(new Type[] { typeof(char) });

                // Multiple characters
                else
                    optCtorInfo = typeof(OptionAttribute).GetConstructor(new Type[] { typeof(string) });

                var optHelpPropInfo = typeof(OptionAttribute).GetProperty("HelpText");
                var optDefaultInfo = typeof(OptionAttribute).GetProperty("Default");
                var optRequiredInfo = typeof(OptionAttribute).GetProperty("Required");
                var attBuilder = new CustomAttributeBuilder(optCtorInfo,
                                        new object[] { option.Name.Length == 1? (object) option.Name[0] : option.Name },
                                        new PropertyInfo[] { optHelpPropInfo, optDefaultInfo, optRequiredInfo },
                                        // Booleans are always optional
                                        new object[] { option.Description, optionValue, option.Value is bool? false : option.Required && optionEmpty });
                pluginOptionProperty.SetCustomAttribute(attBuilder);
            }
            return pluginOptionClass.CreateTypeInfo().AsType();
        }

        // Get plugin option classes
        public static Type[] GetPluginOptionTypes() {
            // Don't do anything if there are no loaded plugins
            try {
                var plugins = PluginManager.AvailablePlugins;

                if (!plugins.Any())
                    return Array.Empty<Type>();

                // Create CommandLine-friendly option classes for each plugin
                return plugins.Select(p => CreateOptionsFromPlugin(p)).ToArray();
            }
            catch (InvalidOperationException ex) {
                Console.Error.WriteLine(ex.Message);
                Environment.Exit(1);

                return null;
            }
        }

        // Parse all options for all plugins
        public static bool ParsePluginOptions(IEnumerable<string> pluginOptions, Type[] optionsTypes) {

            var hasErrors = false;

            // Run CommandLine parser on each set of plugin options
            foreach (var options in pluginOptions) {

                var selectedPlugin = options.Split(' ')[0].ToLower();

                // Cause an error on the first plugin arguments if no plugins are loaded
                if (optionsTypes.Length == 0) {
                    Console.Error.WriteLine($"The requested plugin '{selectedPlugin}' does not exist or is not loaded");
                    hasErrors = true;
                    continue;
                }

                // Parse plugin arguments
                var parser = new Parser(with => {
                    with.HelpWriter = null;
                    with.CaseSensitive = false;
                    with.AutoHelp = false;
                    with.AutoVersion = false;
                });
                var result = parser.ParseArguments(options.Split(' '), optionsTypes);

                // Print plugin help if parsing failed
                if (result is NotParsed<object> notParsed) {
                    if (!(notParsed.Errors.First() is BadVerbSelectedError)) {
                        var helpText = HelpText.AutoBuild(result, h => {
                            h.Heading = $"Usage for plugin '{selectedPlugin}':";
                            h.Copyright = string.Empty;
                            h.AutoHelp = false;
                            h.AutoVersion = false;
                            return h;
                        }, e => e);
                        Console.Error.WriteLine(helpText);
                    } else {
                        Console.Error.WriteLine($"The requested plugin '{selectedPlugin}' does not exist or is not loaded");
                    }
                    hasErrors = true;
                    continue;
                }

                // Get plugin arguments and write them to plugin options class
                var optionsObject = (result as Parsed<object>).Value;
                var plugin = PluginManager.AvailablePlugins.First(p => optionsObject.GetType().FullName == p.Id + "Options");

                foreach (var prop in optionsObject.GetType().GetProperties()) {
                    var targetProp = plugin.Options.First(x => x.Name == prop.Name);
                    var value = prop.GetValue(optionsObject);

                    // TODO: Use IPluginOption.SetFromString() instead

                    // Validate hex strings
                    if (targetProp is IPluginOptionNumber n && n.Style == PluginOptionNumberStyle.Hex) {
                        try {
                            n.Value = Convert.ChangeType(Convert.ToInt64((string) value, 16), n.Value.GetType());
                        }
                        catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is OverflowException) {
                            Console.Error.WriteLine($"Plugin option error: {prop.Name} must be a hex value (optionally starting with '0x'), and less than the maximum value");
                            hasErrors = true;
                        }
                    }

                    // Enums
                    else if (targetProp.Value?.GetType().IsEnum ?? false) {
                        if (Enum.TryParse(targetProp.Value.GetType(), value.ToString(), out var enumValue))
                            targetProp.Value = enumValue;
                        else {
                            Console.Error.Write($"Plugin option error: Invalid enum value when setting '{targetProp.Name}' to '{value}'; ");
                            Console.Error.WriteLine("valid values are: " + string.Join(", ", Enum.GetNames(targetProp.Value.GetType())));
                            hasErrors = true;
                        }
                    }

                    // All other input types
                    else {
                        try {
                            targetProp.Value = value;
                        }
                        catch (Exception ex) {
                            Console.Error.WriteLine($"Plugin option error: {ex.Message} when setting '{targetProp.Name}' to '{value}'");

                            // Output available choices if the failure is on an IPluginOptionChoice<T>
                            if (targetProp.GetType().GetProperty("Choices") is PropertyInfo choiceProp) {
                                var choiceDict = (IDictionary) choiceProp.GetValue(targetProp);
                                Console.Error.WriteLine($"Valid values for '{targetProp.Name}' are: " + string.Join(", ", choiceDict.Keys.Cast<object>()));
                            }

                            hasErrors = true;
                        }
                    }
                }

                // Enable plugin and inform of options
                if (!hasErrors) {
                    var plugins = PluginManager.AsInstance.ManagedPlugins;
                    var managedPlugin = plugins.First(p => p.Plugin == plugin);

                    // Move plugin to end of execution order
                    plugins.Remove(managedPlugin);
                    plugins.Add(managedPlugin);
                    managedPlugin.Enabled = true;

                    PluginManager.OptionsChanged(plugin);
                }
            }
            return !hasErrors;
        }
    }
}
