/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace Il2CppInspector.Cpp.UnityHeaders
{
    // Each instance of UnityHeader represents one header file which potentially covers multiple versions of Unity.
    public class UnityHeader
    {
        // Metadata version of this header. Multiple headers may have the same metadata version
        public double MetadataVersion { get; }

        // Minimum and maximum Unity version numbers corresponding to this header. Both endpoints are inclusive
        public UnityVersionRange Version { get; }

        // Filename for the embedded .h resource file containing the header
        public string HeaderFilename { get; }

        private UnityHeader(string headerFilename) {
            HeaderFilename = headerFilename;
            Version = UnityVersionRange.FromFilename(HeaderFilename);
            MetadataVersion = double.Parse(headerFilename.Split("-")[0], NumberFormatInfo.InvariantInfo);
        }

        public override string ToString() => Version.ToString();

        // Return the contents of this header file as a string
        public string GetHeaderText() {
            string resourceName = typeof(UnityHeader).Namespace + "." + HeaderFilename;
            Assembly assembly = Assembly.GetCallingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                throw new FileNotFoundException(resourceName);
            }
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            return result;
        }

        // List all header files embedded into this build of Il2CppInspector
        public static IEnumerable<UnityHeader> GetAllHeaders() {
            string prefix = typeof(UnityHeader).Namespace + ".";
            Assembly assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceNames()
                .Where(s => s.StartsWith(prefix) && s.EndsWith(".h"))
                .Select(s => new UnityHeader(s.Substring(prefix.Length)));
        }

        // List all API header files and versions embedded into this build of Il2CppInspector
        public static IEnumerable<(string resourceName, UnityVersion minVersion, UnityVersion maxVersion)> GetAPIList() {
            string prefix = "Il2CppInspector.Cpp.Il2CppAPIHeaders.";
            Assembly assembly = Assembly.GetExecutingAssembly();
            var versions = new List<(string resourceName, UnityVersion minVersion, UnityVersion maxVersion)>();

            foreach (var headerFilename in assembly.GetManifestResourceNames().Where(s => s.StartsWith(prefix) && s.EndsWith(".h"))) {
                var bits = headerFilename.Substring(prefix.Length).Replace(".h", "").Split("-");
                var min = new UnityVersion(bits[0]);
                UnityVersion max = min;
                if (bits.Length == 2 && bits[1] != "")
                    max = new UnityVersion(bits[1]);
                versions.Add((headerFilename, min, max));
            }
            return versions;
        }

        // Get the header file which supports the given version of Unity
        public static UnityHeader GetHeaderForVersion(string version) => GetHeaderForVersion(new UnityVersion(version));
        public static UnityHeader GetHeaderForVersion(UnityVersion version) => GetAllHeaders().First(h => h.Version.Contains(version));

        public static string GetAPIResourceNameForVersion(UnityVersion version) =>
            GetAPIList().First(v => version.CompareTo(v.minVersion) >= 0 && (v.maxVersion == null || version.CompareTo(v.maxVersion) <= 0)).resourceName;

        public static string GetAPITextForVersion(UnityVersion version) {
            var apiResource = GetAPIResourceNameForVersion(version);
            Assembly assembly = Assembly.GetCallingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(apiResource);
            if (stream == null) {
                throw new FileNotFoundException(apiResource);
            }
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            return result;
        }

        // Guess which header file(s) correspond to the given metadata+binary.
        // Note that this may match multiple headers due to structural changes between versions
        // that are not reflected in the metadata version.
        public static List<UnityHeader> GuessHeadersForModel(Reflection.TypeModel model) {
            List<UnityHeader> result = new List<UnityHeader>();
            foreach (var h in GetAllHeaders()) {
                if (h.MetadataVersion != model.Package.BinaryImage.Version)
                    continue;
                if (h.MetadataVersion == 21) {
                    /* Special version logic for metadata version 21 based on the Il2CppMetadataRegistration.fieldOffsets field */
                    var headerFieldOffsetsArePointers = h.Version.Min.CompareTo("5.3.7") >= 0 && h.Version.Min.CompareTo("5.4.0") != 0;
                    var binaryFieldOffsetsArePointers = model.Package.Binary.FieldOffsets == null;
                    if (headerFieldOffsetsArePointers != binaryFieldOffsetsArePointers)
                        continue;
                }
                result.Add(h);
            }
            return result;
        }
    }
}
