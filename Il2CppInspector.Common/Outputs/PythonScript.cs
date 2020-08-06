// Copyright (c) 2019-2020 Carter Bush - https://github.com/carterbush
// Copyright (c) 2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
// Copyright 2020 Robert Xiao - https://robertxiao.ca/
// All rights reserved

using System.Linq;
using System.IO;
using Il2CppInspector.Reflection;
using Il2CppInspector.Model;
using System.Collections.Generic;
using System;

namespace Il2CppInspector.Outputs
{
    public class PythonScript
    {
        private readonly AppModel model;

        public PythonScript(AppModel model) => this.model = model;

        // Get list of available script targets
        public static IEnumerable<string> GetAvailableTargets() {
            var ns = typeof(PythonScript).Namespace + ".ScriptResources.Targets";
            var res = ResourceHelper.GetNamesForNamespace(ns);
            return res.Select(s => Path.GetFileNameWithoutExtension(s.Substring(ns.Length + 1))).OrderBy(s => s);
        }
        
        // Output script file
        public void WriteScriptToFile(string outputFile, string target, string existingTypeHeaderFIle = null, string existingJsonMetadataFile = null) {

            // Check that target script API is valid
            if (!GetAvailableTargets().Contains(target))
                throw new InvalidOperationException("Unknown script API target: " + target);

            // Write types file first if it hasn't been specified
            var typeHeaderFile = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + ".h");

            if (string.IsNullOrEmpty(existingTypeHeaderFIle))
                writeTypes(typeHeaderFile);
            else
                typeHeaderFile = existingTypeHeaderFIle;

            var typeHeaderRelativePath = getRelativePath(outputFile, typeHeaderFile);

            // Write JSON metadata if it hasn't been specified
            var jsonMetadataFile = Path.Combine(Path.GetDirectoryName(outputFile), Path.GetFileNameWithoutExtension(outputFile) + ".json");

            if (string.IsNullOrEmpty(existingJsonMetadataFile))
                writeJsonMetadata(jsonMetadataFile);
            else
                jsonMetadataFile = existingJsonMetadataFile;

            var jsonMetadataRelativePath = getRelativePath(outputFile, jsonMetadataFile);

            var ns = typeof(PythonScript).Namespace + ".ScriptResources";
            var preamble = ResourceHelper.GetText(ns + ".shared-preamble.py");
            var main = ResourceHelper.GetText(ns + ".shared-main.py");
            var api = ResourceHelper.GetText($"{ns}.Targets.{target}.py");

            var script = string.Join("\n", new [] { preamble, api, main })
                .Replace("%SCRIPTFILENAME%", Path.GetFileName(outputFile))
                .Replace("%TYPE_HEADER_RELATIVE_PATH%", typeHeaderRelativePath.ToEscapedString())
                .Replace("%JSON_METADATA_RELATIVE_PATH%", jsonMetadataRelativePath.ToEscapedString())
                .Replace("%TARGET_UNITY_VERSION%", model.UnityHeaders.ToString());

            File.WriteAllText(outputFile, script);
        }

        private void writeTypes(string typeHeaderFile) => new CppScaffolding(model).WriteTypes(typeHeaderFile);

        private void writeJsonMetadata(string jsonMetadataFile) => new JSONMetadata(model).Write(jsonMetadataFile);

        private string getRelativePath(string from, string to) =>
            Path.GetRelativePath(Path.GetDirectoryName(Path.GetFullPath(from)),
                                           Path.GetDirectoryName(Path.GetFullPath(to)))
                                         + Path.DirectorySeparatorChar
                                         + Path.GetFileName(to);
    }
}
