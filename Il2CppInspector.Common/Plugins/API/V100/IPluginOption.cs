/*
    Copyright 2020-2021 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Il2CppInspector.PluginAPI.V100
{
    /// <summary>
    /// Interface representing a plugin option
    /// </summary>
    public interface IPluginOption
    {
        /// <summary>
        /// Option name for CLI and unique ID
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Option description for GUI
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// True if the setting is required for the plugin to function, false if optional
        /// Optional by default
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// The default value of the option, if any
        /// Becomes the current value of the option when supplied by the user
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// A condition that determines whether the option is enabled,
        /// based on the settings of other options or any other desired criteria
        /// </summary>
        public Func<bool> If { get; set; }

        /// <summary>
        /// Set an option from a string value
        /// </summary>
        public void SetFromString(string value);
    }

    /// <summary>
    /// Defines how to display lists of choices in the GUI
    /// Dropdown for a dropdown box, List for a list of radio buttons
    /// </summary>
    public enum PluginOptionChoiceStyle
    {
        Dropdown,
        List
    }

    /// <summary>
    /// Defines how to display and parse numbers in the CLI and GUI
    /// Decimal for regular numbers, Hex for hexadecimal strings
    /// </summary>
    public enum PluginOptionNumberStyle
    {
        Decimal,
        Hex
    }

    /// <summary>
    /// The base option from which all other options are derived
    /// </summary>
    public abstract class PluginOption<T> : IPluginOption
    {
        /// <summary>
        /// The name of the option as it will be supplied in an argument on the command-line
        /// If you specify a single character, the single-dash syntax "-x" can be used instead of "--xxxxxx"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A description of what the option does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// True if the option must be specified, false if optional
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// When created, the default value of the option
        /// During plugin execution, the current value of the option
        /// </summary>
        private T _value;
        object IPluginOption.Value { get => Value; set => Value = (T) value; }
        public T Value {
            get => _value;
            set {
                // Disabled options can be set to invalid values
                if (!If()) {
                    _value = value;
                    return;
                }

                // Perform internal validation
                InternalValidate(value);

                // Perform optional user-supplied validation
                Validate?.Invoke(value);

                // Save changes
                _value = value;
            }
        }

        /// <summary>
        /// Set an option from a string value
        /// </summary>
        public abstract void SetFromString(string value);

        /// <summary>
        /// This can be set to a predicate that determines whether the option is enabled in the GUI
        /// By default, enable all options unless overridden
        /// </summary>
        public Func<bool> If { get; set; } = () => true;

        /// <summary>
        /// Optional validation function for the option in addition to basic automatic validation
        /// Must either throw an exception or return true
        /// </summary>
        public Func<T, bool> Validate;

        // Internal validation of each option (for internal use only)
        protected abstract void InternalValidate(T value);
    }

    /// <summary>
    /// Numeric type option (for internal use only)
    /// </summary>
    public interface IPluginOptionNumber
    {
        /// <summary>
        /// The style of the number
        /// </summary>
        public PluginOptionNumberStyle Style { get; set; }

        /// <summary>
        /// The value of the number
        /// </summary>
        object Value { get; set; }
    }

    /// <summary>
    /// Option representing a text string
    /// </summary>
    public class PluginOptionText : PluginOption<string>
    {
        protected sealed override void InternalValidate(string text) {
            // Don't allow required text to be empty
            if (Required && string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty");
        }

        public override void SetFromString(string value) => Value = value;
    }

    /// <summary>
    /// Option representing a file path
    /// </summary>
    public class PluginOptionFilePath : PluginOption<string>
    {
        protected sealed override void InternalValidate(string path) {
            // Don't allow required path name to be empty
            if (Required && string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path name is required");

            // Only validate a non-required path name if it is non-empty
            if (!Required && string.IsNullOrEmpty(path))
                return;

            // Throws an exception if the path is invalid (file or folder may or may not exist)
            var fullPath = Path.GetFullPath(path);
            var fileName = Path.GetFileName(fullPath);

            if (IsFolder) {
                if (MustExist && !Directory.Exists(fullPath))
                    throw new ArgumentException($"Directory {fileName} does not exist");
                if (MustNotExist && Directory.Exists(fullPath))
                    throw new ArgumentException($"Directory {fileName} already exists");
            }
            else {
                if (MustExist && !File.Exists(fullPath))
                    throw new ArgumentException($"File {fileName} does not exist");
                if (MustNotExist && File.Exists(fullPath))
                    throw new ArgumentException($"File {fileName} already exists");
            }

            // Check selected file has a valid extension
            if (!IsFolder && !AllowedExtensions.ContainsKey("*")) {
                var ext = Path.GetExtension(fullPath).ToLower();
                if (ext.StartsWith('.'))
                    ext = ext.Substring(1);

                if (!AllowedExtensions.ContainsKey(ext))
                    throw new ArgumentException($"File {fileName} has an invalid filename extension");
            }
        }

        /// <summary>
        /// Set this to true if you want the user to specify a folder rather than a file
        /// </summary>
        public bool IsFolder = false;

        /// <summary>
        /// Set this to true if the specified file or folder must exist
        /// </summary>
        public bool MustExist = false;

        /// <summary>
        /// Set this to true if the specified file or folder must not exist
        /// </summary>
        public bool MustNotExist = false;

        /// <summary>
        /// List of file extensions to allow with descriptions for GUI, when selecting a file
        /// Specify * to allow all extensions
        /// </summary>
        public Dictionary<string, string> AllowedExtensions = new Dictionary<string, string> {
            ["*"] = "All files"
        };

        public override void SetFromString(string value) => Value = value;
    }

    /// <summary>
    /// And option representing boolean true or false (yes/no, on/off etc.)
    /// </summary>
    public class PluginOptionBoolean : PluginOption<bool>
    {
        protected sealed override void InternalValidate(bool value) { }

        public override void SetFromString(string value) => Value = bool.Parse(value);
    }

    /// <summary>
    /// Option representing a number
    /// </summary>
    /// <typeparam name="T">The type of the number</typeparam>
    public class PluginOptionNumber<T> : PluginOption<T>, IPluginOptionNumber where T : struct
    {
        /// <summary>
        /// Decimal for normal numbers
        /// Hex to display and parse numbers as hex in the CLI and GUI
        /// </summary>
        public PluginOptionNumberStyle Style { get; set; }

        /// <summary>
        /// The value of the number
        /// </summary>
        object IPluginOptionNumber.Value { get => Value; set => Value = (T) value; }

        protected sealed override void InternalValidate(T value) { }

        public override void SetFromString(string value) {
            Value = (T) Convert.ChangeType(Convert.ToInt64(value, Style == PluginOptionNumberStyle.Hex? 16 : 10), typeof(T));
        }
    }

    /// <summary>
    /// Option representing a single choice from a list of choices
    /// </summary>
    public class PluginOptionChoice<T> : PluginOption<T>
    {
        /// <summary>
        /// List of items to choose from
        /// The Keys are the actual values and those supplied via the CLI
        /// The Values are descriptions of each value shown in the GUI
        /// </summary>
        public Dictionary<T, string> Choices { get; set; }

        /// <summary>
        /// Dropdown to display the list as a drop-down box in the GUI
        /// List to display the list as a set of grouped radio buttons in the GUI
        /// </summary>
        public PluginOptionChoiceStyle Style { get; set; }

        protected sealed override void InternalValidate(T value) {
            // Allow Choices to be null so that setting Value first on init doesn't throw an exception
            if (!Choices?.Keys.Contains(value) ?? false)
                throw new ArgumentException("Specified choice is not one of the available choices");
        }

        public override void SetFromString(string value) {
            if (typeof(T).IsEnum)
                Value = (T) Enum.Parse(typeof(T), value);
            else
                Value = (T) Convert.ChangeType(value, typeof(T));
        }
    }
}