using System;
using System.IO;

namespace Save
{
    public static class SafePath
    {
        public static bool IsSafeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name == "." || name == "..") return false;

            return name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && name.IndexOfAny(new[] { '/', '\\', ':' }) < 0
                   && !Path.IsPathRooted(name);
        }

        public static bool TryCombine(string folder, string name, out string path)
        {
            path = null;

            if (string.IsNullOrEmpty(folder) || !IsSafeName(name)) return false;

            string root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(root, name));

            if (full.Length <= root.Length || !full.StartsWith(root, StringComparison.Ordinal)) return false;

            path = full;
            return true;
        }
    }
}
