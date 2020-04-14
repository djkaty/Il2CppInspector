using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Il2CppInspector.Outputs.UnityHeaders
{
    public class UnityHeader
    {
        public double MetadataVersion { get; }
        public UnityVersion MinVersion { get; }
        public UnityVersion MaxVersion { get; }
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

        public bool Contains(UnityVersion version) {
            return version.CompareTo(MinVersion) >= 0 && (MaxVersion == null || version.CompareTo(MaxVersion) <= 0);
        }

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

        public static IEnumerable<UnityHeader> GetAllHeaders() {
            string prefix = typeof(UnityHeader).Namespace + ".";
            Assembly assembly = Assembly.GetCallingAssembly();
            return assembly.GetManifestResourceNames()
                .Where(s => s.StartsWith(prefix) && s.EndsWith(".h"))
                .Select(s => new UnityHeader(s.Substring(prefix.Length)));
        }

        public static UnityHeader GetHeaderForVersion(string version) => GetHeaderForVersion(new UnityVersion(version));
        public static UnityHeader GetHeaderForVersion(UnityVersion version) {
            return GetAllHeaders().Where(v => v.Contains(version)).First();
        }

        public static List<UnityHeader> GuessHeadersForModel(Reflection.Il2CppModel model) {
            List<UnityHeader> result = new List<UnityHeader>();
            foreach (var v in GetAllHeaders()) {
                if (v.MetadataVersion != model.Package.BinaryImage.Version)
                    continue;
                if (v.MetadataVersion == 21) {
                    /* Special version logic for metadata version 21 based on the Il2CppMetadataRegistration.fieldOffsets field */
                    var headerFieldOffsetsArePointers = (v.MinVersion.CompareTo("5.3.7") >= 0 && v.MinVersion.CompareTo("5.4.0") != 0);
                    var binaryFieldOffsetsArePointers = (model.Package.Binary.FieldOffsets == null);
                    if (headerFieldOffsetsArePointers != binaryFieldOffsetsArePointers)
                        continue;
                }
                result.Add(v);
            }
            return result;
        }
    }
}
