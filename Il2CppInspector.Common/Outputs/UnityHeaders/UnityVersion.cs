using System;
using System.Text.RegularExpressions;

namespace Il2CppInspector.Outputs.UnityHeaders
{
    public class UnityVersion : IComparable<UnityVersion>, IEquatable<UnityVersion>
    {
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

        public int CompareTo(UnityVersion other) {
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

        public override bool Equals(object obj) {
            return Equals(obj as UnityVersion);
        }

        public bool Equals(UnityVersion other) {
            return other != null &&
                   Major == other.Major &&
                   Minor == other.Minor &&
                   Update == other.Update &&
                   BuildType == other.BuildType &&
                   BuildNumber == other.BuildNumber;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Major, Minor, Update, BuildType, BuildNumber);
        }
    }
}
