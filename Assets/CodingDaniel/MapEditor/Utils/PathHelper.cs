using System;
using System.Collections.Generic;
using System.IO;

namespace CodingDaniel.MapEditor.Utils
{
    public static class PathHelper
    {
        public static bool IsPathRooted(string path)
        {
            return Path.IsPathRooted(path);
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static string RemoveInvalidFileNameCharacters(string name)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; ++i)
            {
                name = name.Replace(invalidChars[i].ToString(), string.Empty);
            }
            return name;
        }

        public static string GetUniqueName(string desiredName, string ext, List<string> existingNames, bool noSpace = false)
        {
            if (existingNames == null || existingNames.Count == 0)
            {
                return desiredName;
            }

            for (int i = 0; i < existingNames.Count; ++i)
            {
                existingNames[i] = existingNames[i].ToLower();
            }

            HashSet<string> existingNamesHS = new HashSet<string>(existingNames);
            if (string.IsNullOrEmpty(ext))
            {
                if (!existingNamesHS.Contains(desiredName.ToLower()))
                {
                    return desiredName;
                }
            }
            else
            {
                if (!existingNamesHS.Contains(string.Format("{0}{1}", desiredName.ToLower(), ext)))
                {
                    return desiredName;
                }
            }

            string[] parts = desiredName.Split(' ');
            string lastPart = parts[parts.Length - 1];
            int number;
            if (!int.TryParse(lastPart, out number))
            {
                number = 1;
            }
            else
            {
                desiredName = desiredName.Substring(0, desiredName.Length - lastPart.Length).TrimEnd(' ');
            }

            const int maxAttempts = 10000;
            for (int i = 0; i < maxAttempts; ++i)
            {
                string uniqueName;
                if (string.IsNullOrEmpty(ext))
                {
                    uniqueName = string.Format("{0} {1}", desiredName, number);
                }
                else
                {
                    uniqueName = string.Format("{0} {1}{2}", desiredName, number, ext);
                }

                if(noSpace)
                {
                    uniqueName = uniqueName.Replace(" ", "");
                }

                if (!existingNamesHS.Contains(uniqueName.ToLower()))
                {
                    if(noSpace)
                    {
                        return string.Format("{0} {1}", desiredName, number).Replace(" ", "");
                    }
                    else
                    {
                        return string.Format("{0} {1}", desiredName, number);
                    }
                }

                number++;
            }

            return string.Format("{0} {1}", desiredName, Guid.NewGuid().ToString("N"));
        }

        public static string GetUniqueName(string desiredName, List<string> existingNames)
        {
            return GetUniqueName(desiredName, null, existingNames);
        }
    }
}
