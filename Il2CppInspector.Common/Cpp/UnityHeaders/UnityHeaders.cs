/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Cpp.UnityHeaders
{
    // Each instance of UnityHeaders represents all of the header files needed to build for a specific range of Unity versions
    // Also provides helper functions to fetch various types of resources
    public class UnityHeaders : IEquatable<UnityHeaders>
    {
        // Metadata version for which this group of headers are valid. Multiple headers may have the same metadata version
        public double MetadataVersion { get; }

        // Range of Unity versions for which this group of headers are valid
        public UnityVersionRange VersionRange { get; }

        // The fully qualified names of the embedded resources
        public UnityResource TypeHeaderResource { get; }
        public UnityResource APIHeaderResource { get; }

        // Initialize from a type header and an API header
        private UnityHeaders(UnityResource typeHeaders, UnityResource apiHeaders) {
            TypeHeaderResource = typeHeaders;
            APIHeaderResource = apiHeaders;

            VersionRange = typeHeaders.VersionRange.Intersect(apiHeaders.VersionRange);
            MetadataVersion = GetMetadataVersionFromFilename(typeHeaders.Name);
        }

        // Return the contents of the type header file as a string
        public string GetTypeHeaderText(int WordSize) {
            var str = (WordSize == 32 ? "#define IS_32BIT\n" : "") + TypeHeaderResource.GetText();
            
            // Versions 5.3.6-5.4.6 don't include a definition for VirtualInvokeData
            if (VersionRange.Min.CompareTo("5.3.6") >= 0 && VersionRange.Max?.CompareTo("5.4.6") <= 0) {
                str = str + @"struct VirtualInvokeData
{
    Il2CppMethodPointer methodPtr;
    const MethodInfo* method;
};";
            }
            return str;
        }

        // Return the contents of the API header file as a string
        public string GetAPIHeaderText() => APIHeaderResource.GetText();

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

            // Get total range of selected headers
            // Sort is needed because 5.x.x comes before 20xx.x.x in the resource list
            typeHeaders = typeHeaders.OrderBy(x => x.VersionRange).ToList();
            var totalRange = new UnityVersionRange(typeHeaders.First().VersionRange.Min, typeHeaders.Last().VersionRange.Max);

            // Get all API versions in this range
            var apis = GetAllAPIHeaders().Where(a => a.VersionRange.Intersect(totalRange) != null).ToList();

            // Get the API exports for the binary
            var exports = binary.GetAPIExports();

            // No il2cpp exports? Just return the earliest version from the header range
            // The API version may be incorrect but should be a subset of the real API and won't cause C++ compile errors
            if (!exports.Any()) {
                Console.WriteLine("No IL2CPP API exports found in binary - IL2CPP APIs will be unavailable in C++ project");

                return typeHeaders.Select(t => new UnityHeaders(t, 
                    apis.Last(a => a.VersionRange.Intersect(t.VersionRange) != null))).ToList();
            }

            // Go through all of the possible API versions and see how closely they match the binary
            // Note: if apis.Count == 1, we can't actually narrow down the version range further,
            // but we still need to check that the APIs actually exist in the binary
            var apiMatches = new List<UnityResource>();
            foreach (var api in apis) {
                var apiFunctionList = GetFunctionNamesFromAPIHeaderText(api.GetText());
                
                // Every single function in the API list must be an export for a match
                if (!apiFunctionList.Except(exports.Keys).Any()) {
                    apiMatches.Add(api);
                }
            }

            if (apiMatches.Any()) {
                // Intersect all API ranges with all header ranges to produce final list of possible ranges
               Console.WriteLine("IL2CPP API discovery was successful");

                return typeHeaders.SelectMany(
                    t => apiMatches.Where(a => t.VersionRange.Intersect(a.VersionRange) != null)
                             .Select(a => new UnityHeaders(t, a))).ToList();
            }

            // None of the possible API versions match the binary
            // Select the oldest API version from the group - C++ project compilation will fail
            Console.WriteLine("No exact match for IL2CPP APIs found in binary - IL2CPP API availability in C++ project will be partial");

            return typeHeaders.Select(t => new UnityHeaders(t, 
                apis.Last(a => a.VersionRange.Intersect(t.VersionRange) != null))).ToList();
        }

        // Convert il2cpp-api-functions.h from "DO_API(r, n, p)" to "typedef r (*n)(p)"
        private static readonly Regex APILineRegex = new Regex(@"^DO_API(?:_NO_RETURN)?\((.*?),(.*?),\s*\((.*?)\)\s*\);", RegexOptions.Multiline);

        internal static string GetFunctionNameFromAPILine(string line) {
            var match = APILineRegex.Match(line);
            return match.Success ? match.Groups[2].ToString().Trim() : string.Empty;
        }

        internal static string GetTypedefsFromAPIHeader(string text) => APILineRegex.Replace(text, "typedef $1 (*$2)($3);");

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

        // Equality comparisons
        public static bool operator ==(UnityHeaders first, UnityHeaders second) {
            if (ReferenceEquals(first, second))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;
            return first.VersionRange.Equals(second.VersionRange);
        }

        public static bool operator !=(UnityHeaders first, UnityHeaders second) => !(first == second);

        public override bool Equals(object obj) => Equals(obj as UnityHeaders);

        public bool Equals(UnityHeaders other) => VersionRange == other?.VersionRange;

        public override int GetHashCode() => VersionRange.GetHashCode();
    }
}
