/*
    Copyright 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty

    All rights reserved.
*/

using System;
using System.Collections.Generic;
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
                // Perform internal validation
                InternalValidate(value);

                // Perform optional user-supplied validation
                Validate?.Invoke(value);

                // Save changes
                _value = value;
            }
        }

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
    }

    /// <summary>
    /// Option representing a file path
    /// </summary>
    public class PluginOptionFilePath : PluginOption<string>
    {
        // Don't asllow required path name to be empty
        protected sealed override void InternalValidate(string path) {
            if (Required && string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path name is required");

            // Throws an exception if the path is invalid (file or folder may or may not exist)
            System.IO.Path.GetFullPath(path ?? "");
        }
    }

    /// <summary>
    /// And option representing boolean true or false (yes/no, on/off etc.)
    /// </summary>
    public class PluginOptionBoolean : PluginOption<bool>
    {
        protected sealed override void InternalValidate(bool value) { }
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
    }
}