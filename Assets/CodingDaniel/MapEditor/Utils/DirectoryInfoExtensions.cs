using System.IO;

namespace CodingDaniel.MapEditor.Utils
{
    public static class DirectoryInfoExtensions
    {
        public static void DeepCopy(this DirectoryInfo directory, string destinationDir)
        {
            string basePath = Path.Combine(destinationDir, directory.Name);
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath); 
                
            }
            foreach (string dir in Directory.GetDirectories(directory.FullName, "*", SearchOption.AllDirectories)) 
            {
                string dirToCreate = dir.Replace(directory.FullName, Path.Combine(destinationDir,directory.Name)); 
                Directory.CreateDirectory(dirToCreate); 
            } 
            
            foreach (string newPath in Directory.GetFiles(directory.FullName, "*.*", SearchOption.AllDirectories)) 
            { 
                File.Copy(newPath, newPath.Replace(directory.FullName, Path.Combine(destinationDir,directory.Name)), true); 
            } 
        } 
    }
}