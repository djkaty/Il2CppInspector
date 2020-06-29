/*
    Copyright 2017-2020 Katy Coe - http://www.hearthcode.org - http://www.djkaty.com
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Il2CppInspector.Cpp.UnityHeaders
{
    // Each instance of UnityHeader represents one header file which potentially covers multiple versions of Unity.
    public class UnityHeader
    {
        // Metadata version of this header. Multiple headers may have the same metadata version
        public double MetadataVersion { get; }

        // Minimum and maximum Unity version numbers corresponding to this header. Both endpoints are inclusive
        public UnityVersion MinVersion { get; }
        public UnityVersion MaxVersion { get; }

        // Filename for the embedded .h resource file containing the header
        public string HeaderFilename { get; }

        private UnityHeader(string headerFilename) {
            HeaderFilename = headerFilename;
            var bits = headerFilename.Replace(".h", "").Split("-");
            MetadataVersion = double.Parse(bits[0]);
            MinVersion = new UnityVersion(bits[1]);
            if (bits.Length == 2)
                MaxVersion = MinVersion;
            else if (bits[2] != "")
                MaxVersion = new UnityVersion(bits[2]);
        }

        public override string ToString() {
            var res = $"{MinVersion}";
            if (MaxVersion == null)
                res += "+";
            else if (MaxVersion != MinVersion)
                res += $" - {MaxVersion}";
            return res;
        }

        // Determine if this header supports the given version of Unity
        public bool Contains(UnityVersion version) => version.CompareTo(MinVersion) >= 0 && (MaxVersion == null || version.CompareTo(MaxVersion) <= 0);

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

        // List all header files embedded into this build of Il2Cpp
        public static IEnumerable<UnityHeader> GetAllHeaders() {
            string prefix = typeof(UnityHeader).Namespace + ".";
            Assembly assembly = Assembly.GetCallingAssembly();
            return assembly.GetManifestResourceNames()
                .Where(s => s.StartsWith(prefix) && s.EndsWith(".h"))
                .Select(s => new UnityHeader(s.Substring(prefix.Length)));
        }

        // Get the header file which supports the given version of Unity
        public static UnityHeader GetHeaderForVersion(string version) => GetHeaderForVersion(new UnityVersion(version));
        public static UnityHeader GetHeaderForVersion(UnityVersion version) => GetAllHeaders().Where(v => v.Contains(version)).First();

        // Guess which header file(s) correspond to the given metadata+binary.
        // Note that this may match multiple headers due to structural changes between versions
        // that are not reflected in the metadata version.
        public static List<UnityHeader> GuessHeadersForModel(Reflection.Il2CppModel model) {
            List<UnityHeader> result = new List<UnityHeader>();
            foreach (var v in GetAllHeaders()) {
                if (v.MetadataVersion != model.Package.BinaryImage.Version)
                    continue;
                if (v.MetadataVersion == 21) {
                    /* Special version logic for metadata version 21 based on the Il2CppMetadataRegistration.fieldOffsets field */
                    var headerFieldOffsetsArePointers = v.MinVersion.CompareTo("5.3.7") >= 0 && v.MinVersion.CompareTo("5.4.0") != 0;
                    var binaryFieldOffsetsArePointers = model.Package.Binary.FieldOffsets == null;
                    if (headerFieldOffsetsArePointers != binaryFieldOffsetsArePointers)
                        continue;
                }
                result.Add(v);
            }
            return result;
        }
    }
}
