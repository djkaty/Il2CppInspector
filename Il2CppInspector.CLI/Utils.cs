// Copyright (c) 2017-2020 Katy Coe - https://www.djkaty.com - https://github.com/djkaty
// All rights reserved

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Il2CppInspector
{
    public class Utils
    {
        public static string FindPath(string pathWithWildcards) {
            var absolutePath = Path.GetFullPath(pathWithWildcards);

            if (absolutePath.IndexOf("*", StringComparison.Ordinal) == -1)
                return absolutePath;

            Regex sections = new Regex(@"((?:[^*]*)\\)((?:.*?)\*.*?)(?:$|\\)");
            var matches = sections.Matches(absolutePath);

            var pathLength = 0;
            var path = "";
            foreach (Match match in matches) {
                path += match.Groups[1].Value;
                var search = match.Groups[2].Value;

                if (!Directory.Exists(path))
                    return null;

                var dir = Directory.GetDirectories(path, search, SearchOption.TopDirectoryOnly)
                    .OrderByDescending(x => x)
                    .FirstOrDefault();

                path = dir + @"\";
                pathLength += match.Groups[1].Value.Length + match.Groups[2].Value.Length + 1;
            }

            if (pathLength < absolutePath.Length)
                path += absolutePath.Substring(pathLength);

            return path;
        }
    }
}