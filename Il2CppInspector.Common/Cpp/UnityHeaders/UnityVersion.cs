/*
    Copyright 2017-2020 Katy Coe - http://www.djkaty.com - https://github.com/djkaty
    Copyright 2020 Robert Xiao - https://robertxiao.ca

    All rights reserved.
*/

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Cpp.UnityHeaders
{
    // Parsed representation of a Unity version number, such as 5.3.0f1 or 2019.3.7.
    public class UnityVersion : IComparable<UnityVersion>, IEquatable<UnityVersion>
    {
        // A sorted enumeration of build types, in order of maturity
        public enum BuildTypeEnum
        {
            Unspecified,
            Alpha,
            Beta,
            ReleaseCandidate,
            Final,
            Patch,
        }

        public static string BuildTypeToString(BuildTypeEnum buildType) => buildType switch
        {
            BuildTypeEnum.Unspecified => "",
            BuildTypeEnum.Alpha => "a",
            BuildTypeEnum.Beta => "b",
            BuildTypeEnum.ReleaseCandidate => "rc",
            BuildTypeEnum.Final => "f",
            BuildTypeEnum.Patch => "p",
            _ => throw new ArgumentException(),
        };

        public static BuildTypeEnum StringToBuildType(string s) => s switch
        {
            "" => BuildTypeEnum.Unspecified,
            "a" => BuildTypeEnum.Alpha,
            "b" => BuildTypeEnum.Beta,
            "rc" => BuildTypeEnum.ReleaseCandidate,
            "f" => BuildTypeEnum.Final,
            "p" => BuildTypeEnum.Patch,
            _ => throw new ArgumentException("Unknown build type " + s),
        };

        // Unity version number is of the form <Major>.<Minor>.<Update>[<BuildType><BuildNumber>]
        public int Major { get; }
        public int Minor { get; }
        public int Update { get; }
        public BuildTypeEnum BuildType { get; }
        public int BuildNumber { get; }

        public UnityVersion(string versionString) {
            var match = Regex.Match(versionString, @"^(\d+)\.(\d+)(?:\.(\d+))?(?:([a-zA-Z]+)(\d+))?$");
            if (!match.Success)
                throw new ArgumentException($"'${versionString}' is not a valid Unity version number.");
            Major = int.Parse(match.Groups[1].Value);
            Minor = int.Parse(match.Groups[2].Value);
            Update = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            BuildType = match.Groups[4].Success ? StringToBuildType(match.Groups[4].Value) : BuildTypeEnum.Unspecified;
            BuildNumber = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
        }

        public static implicit operator UnityVersion(string versionString) => new UnityVersion(versionString);

        public override string ToString() {
            var res = $"{Major}.{Minor}.{Update}";
            if (BuildType != BuildTypeEnum.Unspecified)
                res += $"{BuildTypeToString(BuildType)}{BuildNumber}";
            return res;
        }

        // Compare two version numbers, intransitively (due to the Unspecified build type)
        public int CompareTo(UnityVersion other) {
            // null means maximum possible version
            if (other == null)
                return -1;
            int res;
            if (0 != (res = Major.CompareTo(other.Major)))
                return res;
            if (0 != (res = Minor.CompareTo(other.Minor)))
                return res;
            if (0 != (res = Update.CompareTo(other.Update)))
                return res;
            // same major.minor.update - if one of these is suffix-less, they compare equal
            // yes, this makes the compare function non-transitive; don't use it to sort things
            if (BuildType == BuildTypeEnum.Unspecified || other.BuildType == BuildTypeEnum.Unspecified)
                return 0;
            if (0 != (res = BuildType.CompareTo(other.BuildType)))
                return res;
            if (0 != (res = BuildNumber.CompareTo(other.BuildNumber)))
                return res;
            return 0;
        }

        // Equality comparisons
        public static bool operator ==(UnityVersion first, UnityVersion second) {
            if (ReferenceEquals(first, second))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;
            return first.Equals(second);
        }

        public static bool operator !=(UnityVersion first, UnityVersion second) => !(first == second);

        public override bool Equals(object obj) => Equals(obj as UnityVersion);

        public bool Equals(UnityVersion other) {
            return other != null &&
                   Major == other.Major &&
                   Minor == other.Minor &&
                   Update == other.Update &&
                   BuildType == other.BuildType &&
                   BuildNumber == other.BuildNumber;
        }

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Update, BuildType, BuildNumber);
    }

    // A range of Unity versions
    public class UnityVersionRange : IComparable<UnityVersionRange>, IEquatable<UnityVersionRange>
    {
        // Minimum and maximum Unity version numbers for this range. Both endpoints are inclusive
        // Max can be null to specify no upper bound
        public UnityVersion Min { get; }
        public UnityVersion Max { get; }

        // Determine if this range contains the specified version
        public bool Contains(UnityVersion version) => version.CompareTo(Min) >= 0 && (Max == null || version.CompareTo(Max) <= 0);

        public UnityVersionRange(UnityVersion min, UnityVersion max) {
            Min = min;
            Max = max;
        }

        // Create a version range from a string, in the format "[Il2CppInspector.Cpp.<namespace-leaf>.][metadataVersion-]<min>-[max].h"
        public static UnityVersionRange FromFilename(string headerFilename) {
            var baseNamespace = "Il2CppInspector.Cpp.";
            headerFilename = headerFilename.Replace(".h", "");

            if (headerFilename.StartsWith(baseNamespace)) {
                headerFilename = headerFilename.Substring(baseNamespace.Length);
                headerFilename = headerFilename.Substring(headerFilename.IndexOf(".") + 1);
            }

            var bits = headerFilename.Split("-");

            // Metadata version supplied
            // Note: This relies on the metadata version being either 2 or 4 characters,
            // and that the smallest Unity version must be 5 characters or more
            if (headerFilename[2] == '-' || headerFilename[4] == '-')
                bits = bits.Skip(1).ToArray();

            var Min = new UnityVersion(bits[0]);
            UnityVersion Max = null;

            if (bits.Length == 1)
                Max = Min;
            if (bits.Length == 2 && bits[1] != "")
                Max = new UnityVersion(bits[1]);

            return new UnityVersionRange(Min, Max);
        }

        // Compare and sort based on the lowest version number
        public int CompareTo(UnityVersionRange other) => Min.CompareTo(other.Min);

        // Intersect two ranges to find the smallest shared set of versions
        // Returns null if the two ranges do not intersect
        // Max == null means no upper bound on version
        public UnityVersionRange Intersect(UnityVersionRange other) {
            var highestLow = Min.CompareTo(other.Min) > 0 ? Min : other.Min;
            var lowestHigh = Max == null? other.Max : Max.CompareTo(other.Max) < 0 ? Max : other.Max;

            if (highestLow.CompareTo(lowestHigh) > 0)
                return null;

            return new UnityVersionRange(highestLow, lowestHigh);
        }

        public override string ToString() {
            var res = $"{Min}";
            if (Max == null)
                res += "+";
            else if (!Max.Equals(Min))
                res += $" - {Max}";
            return res;
        }

        // Equality comparisons
        public static bool operator ==(UnityVersionRange first, UnityVersionRange second) {
            if (ReferenceEquals(first, second))
                return true;
            if (ReferenceEquals(first, null) || ReferenceEquals(second, null))
                return false;
            return first.Equals(second);
        }

        public static bool operator !=(UnityVersionRange first, UnityVersionRange second) => !(first == second);

        public override bool Equals(object obj) => Equals(obj as UnityVersionRange);

        public bool Equals(UnityVersionRange other) => Min.Equals(other?.Min) && Max.Equals(other?.Max);

        public override int GetHashCode() => HashCode.Combine(Min, Max);
    }
}