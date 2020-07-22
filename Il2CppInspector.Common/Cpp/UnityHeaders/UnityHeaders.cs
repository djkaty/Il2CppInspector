/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Cpp.UnityHeaders
{
    // Each instance of UnityHeaders represents all of the header files needed to build for a specific range of Unity versions
    // Also provides helper functions to fetch various types of resources
    public class UnityHeaders
    {
        // Metadata version for which this group of headers are valid. Multiple headers may have the same metadata version
        public double MetadataVersion { get; }

        // Range of Unity versions for which this group of headers are valid
        public UnityVersionRange VersionRange { get; }

        // The fully qualified names of the embedded resources
        private readonly UnityResource typeHeaderResource;
        private readonly UnityResource apiHeaderResource;

        // Initialize from a type header and an API header
        private UnityHeaders(UnityResource typeHeaders, UnityResource apiHeaders) {
            typeHeaderResource = typeHeaders;
            apiHeaderResource = apiHeaders;

            VersionRange = typeHeaders.VersionRange.Intersect(apiHeaders.VersionRange);
            MetadataVersion = GetMetadataVersionFromFilename(typeHeaders.Name);
        }

        // Return the contents of the type header file as a string
        public string GetTypeHeaderText(int WordSize) {
            var str = (WordSize == 32 ? "#define IS_32BIT\n" : "") + typeHeaderResource.GetText();
            
            // Versions 5.3.6-5.4.6 don't include a definition for VirtualInvokeData
            if (VersionRange.Min.CompareTo("5.3.6") >= 0 && VersionRange.Max.CompareTo("5.4.6") <= 0) {
                str = str + @"struct VirtualInvokeData
{
    Il2CppMethodPointer methodPtr;
    const MethodInfo* method;
} VirtualInvokeData;";
            }
            return str;
        }

        // Return the contents of the API header file as a string
        public string GetAPIHeaderText() => apiHeaderResource.GetText();

        // Return the contents of the API header file translated to typedefs as a string
        public string GetAPIHeaderTypedefText() => GetTypedefsFromAPIHeader(GetAPIHeaderText());

        public override string ToString() => VersionRange.ToString();

        // Class which associates an embedded resource with a range of Unity versions
        public class UnityResource
        {
            // The fully qualified name of the embdedded resource
            public string Name { get; }

            // Minimum and maximum Unity version numbers corresponding to this resource. Both endpoints are inclusive
            public UnityVersionRange VersionRange { get; }

            // Get the text of this resource
            public string GetText() => ResourceHelper.GetText(Name);

            public UnityResource(string name) {
                Name = name;
                VersionRange = UnityVersionRange.FromFilename(name);
            }

            public override string ToString() => Name + " for " + VersionRange;
        }

        // Static helpers

        // List all type header files embedded into this build of Il2CppInspector
        public static IEnumerable<UnityResource> GetAllTypeHeaders() =>
            ResourceHelper.GetNamesForNamespace(typeof(UnityHeaders).Namespace)
                .Where(s => s.EndsWith(".h"))
                .Select(s => new UnityResource(s));

        // List all API header files embedded into this build of Il2CppInspector
        public static IEnumerable<UnityResource> GetAllAPIHeaders() =>
            ResourceHelper.GetNamesForNamespace("Il2CppInspector.Cpp.Il2CppAPIHeaders")
                .Where(s => s.EndsWith(".h"))
                .Select(s => new UnityResource(s));

        // Get the headers which support the given version of Unity
        public static UnityHeaders GetHeadersForVersion(UnityVersion version) =>
            new UnityHeaders(GetTypeHeaderForVersion(version), GetAPIHeaderForVersion(version));

        public static UnityResource GetTypeHeaderForVersion(UnityVersion version) => GetAllTypeHeaders().First(r => r.VersionRange.Contains(version));

        // Get the API header file which supports the given version of Unity
        public static UnityResource GetAPIHeaderForVersion(UnityVersion version) => GetAllAPIHeaders().First(r => r.VersionRange.Contains(version));

        // Guess which header file(s) correspond to the given metadata+binary.
        // Note that this may match multiple headers due to structural changes between versions
        // that are not reflected in the metadata version.
        public static List<UnityHeaders> GuessHeadersForBinary(Il2CppBinary binary) {

            List<UnityResource> typeHeaders = new List<UnityResource>();
            foreach (var r in GetAllTypeHeaders()) {
                var metadataVersion = GetMetadataVersionFromFilename(r.Name);

                if (metadataVersion != binary.Image.Version)
                    continue;
                if (metadataVersion == 21) {
                    /* Special version logic for metadata version 21 based on the Il2CppMetadataRegistration.fieldOffsets field */
                    var headerFieldOffsetsArePointers = r.VersionRange.Min.CompareTo("5.3.7") >= 0 && r.VersionRange.Min.CompareTo("5.4.0") != 0;
                    var binaryFieldOffsetsArePointers = binary.FieldOffsets == null;
                    if (headerFieldOffsetsArePointers != binaryFieldOffsetsArePointers)
                        continue;
                }
                typeHeaders.Add(r);
            }

            // TODO: Replace this with an implementation which searches for the correct API header
            return typeHeaders.Select(t => new UnityHeaders(t, GetAPIHeaderForVersion(t.VersionRange.Min))).ToList();
        }

        // Convert il2cpp-api-functions.h from "DO_API(r, n, p)" to "typedef r (*n)(p)"
        internal static string GetTypedefsFromAPIHeader(string text) {
            var rgx = new Regex(@"^DO_API(?:_NO_RETURN)?\((.*?),(.*?),\s*\((.*?)\)\s*\);", RegexOptions.Multiline);
            return rgx.Replace(text, "typedef $1 (*$2)($3);");
        }

        // Get a list of function names from il2cpp-api-functions.h, taking #ifs into account
        private static IEnumerable<string> GetFunctionNamesFromAPIHeaderText(string text) {
            var defText = GetTypedefsFromAPIHeader(text);
            var defs = new CppTypeCollection(32); // word size doesn't matter
            defs.AddFromDeclarationText(defText);

            return defs.TypedefAliases.Keys;
        }

        // Get the metadata version from a type header resource name
        private static double GetMetadataVersionFromFilename(string resourceName)
            => double.Parse(resourceName.Substring(typeof(UnityHeaders).Namespace.Length + 1).Split('-')[0], NumberFormatInfo.InvariantInfo);
    }
}
