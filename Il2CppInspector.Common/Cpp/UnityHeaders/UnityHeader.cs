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
            var str = ResourceHelper.GetText(typeof(UnityHeader).Namespace + "." + HeaderFilename);

            // Versions 5.3.6-5.4.6 don't include a definition for VirtualInvokeData
            if (Version.Min.CompareTo("5.3.6") >= 0 && Version.Max.CompareTo("5.4.6") <= 0) {
                str = str + @"struct VirtualInvokeData
{
    Il2CppMethodPointer methodPtr;
    const MethodInfo* method;
} VirtualInvokeData;";
            }
            return str;
        }

        // List all header files embedded into this build of Il2CppInspector
        public static IEnumerable<UnityHeader> GetAllHeaders() => ResourceHelper.GetNamesForNamespace(typeof(UnityHeader).Namespace)
                .Where(s => s.EndsWith(".h"))
                .Select(s => new UnityHeader(s.Substring(typeof(UnityHeader).Namespace.Length + 1)));

        // List all API header files and versions embedded into this build of Il2CppInspector
        public static Dictionary<string, UnityVersionRange> GetAllAPIs() {
            var list = ResourceHelper.GetNamesForNamespace("Il2CppInspector.Cpp.Il2CppAPIHeaders");
            return list.Select(i => new {
                Key = i,
                Value = UnityVersionRange.FromFilename(i),
            }).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        // Get the header file which supports the given version of Unity
        public static UnityHeader GetHeaderForVersion(string version) => GetHeaderForVersion(new UnityVersion(version));
        public static UnityHeader GetHeaderForVersion(UnityVersion version) => GetAllHeaders().First(h => h.Version.Contains(version));

        // Get API file resources
        public static string GetAPIResourceNameForVersion(UnityVersion version) => GetAllAPIs().First(h => h.Value.Contains(version)).Key;
        public static string GetAPITextForVersion(UnityVersion version) => ResourceHelper.GetText(GetAPIResourceNameForVersion(version));

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
