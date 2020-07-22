/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Il2CppInspector
{
    internal static class ResourceHelper
    {
        // Get a string resource
        public static string GetText(string resourceName) {
            Assembly assembly = Assembly.GetCallingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new FileNotFoundException(resourceName);
            }

            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            return result;
        }

        // Get a list of resources for a namespace
        public static IEnumerable<string> GetNamesForNamespace(string ns) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames().Where(s => s.StartsWith(ns + "."));
        }
    }
}
