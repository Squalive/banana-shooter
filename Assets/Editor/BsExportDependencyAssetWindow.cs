using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class BsExportDependencyAssetWindow : EditorWindow
    {
        [MenuItem("Window/Banana Shooter/Export Dependency")]
        public static void Open()
        {
            GetWindow<BsExportDependencyAssetWindow>();
        }

        private string _srcDirectory;
        private string _dstDirectory;
        
        private void OnGUI()
        {
            GUILayout.Label("Export Asset Directory (stripped out all .meta)", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Source Directory (Inside Assets)");
            _srcDirectory = EditorGUILayout.TextField(_srcDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Source Directory", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // 转换为相对路径（以 Assets/ 开头）
                    if (selected.StartsWith(Application.dataPath))
                    {
                        _srcDirectory = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        Debug.LogWarning("Please select directory inside Assets/");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Destination Directory");
            _dstDirectory = EditorGUILayout.TextField(_dstDirectory);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Output Directory", "", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _dstDirectory = selected;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(_srcDirectory) && !string.IsNullOrEmpty(_dstDirectory))
            {
                if (GUILayout.Button("Export", GUILayout.Height(30)))
                {
                    string srcAbs = Path.Combine(Application.dataPath, _srcDirectory.Substring("Assets/".Length));
                    if (!Directory.Exists(srcAbs))
                    {
                        EditorUtility.DisplayDialog("Error", "Src directory does not exist", "Okay");
                        return;
                    }
                    Export(srcAbs, _dstDirectory);
                }
            }
            
            EditorGUILayout.HelpBox("Export all contents from src directory to dst directory, but all .meta files are stripped away", MessageType.Info);

            ExportRegisteredAssets("Free Sky", "Skybox");
            ExportRegisteredAssets("SimpleFX", "SimpleFX");
            ExportRegisteredAssets("Lowpoly_Crates", "Lowpoly_Crates");
            ExportRegisteredAssets("Fantasy Skybox FREE", "Fantasy Skybox FREE");
        }


        void ExportRegisteredAssets(String assetName, String directoryName)
        {
            var directoryPath = Path.Combine(Application.dataPath, directoryName);
            
            if (Directory.Exists(directoryPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(assetName);
                if (GUILayout.Button("Export"))
                {
                    Export(directoryPath, Path.Combine(Application.dataPath, "..", directoryName));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        static void Export(String src, String dst)
        {
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }
            
            var allFiles = new List<string>();
            GetAllFiles(src, allFiles);
            
            int copiedCount = 0;
            int skippedCount = 0;
            
            try
            {
                foreach (string filePath in allFiles)
                {
                    if (filePath.EndsWith(".meta"))
                    {
                        skippedCount++;
                        continue;
                    }

                    string relPath = filePath.Substring(src.Length + 1); 
                    string destFilePath = Path.Combine(dst, relPath);

                    string destDir = Path.GetDirectoryName(destFilePath);
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    // 复制文件
                    File.Copy(filePath, destFilePath, true);
                    copiedCount++;
                }

                string msg = $"Export successful into {dst}\nCopied file count：{copiedCount}\nskipped .meta file count：{skippedCount}";
                Debug.Log(msg);
                EditorUtility.DisplayDialog("Success", msg, "Okay");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Exceptions threw during exporting：\n{ex.Message}", "Okay");
                Debug.LogError(ex);
            }
        }

        private static void GetAllFiles(string dir, List<string> files)
        {
            try
            {
                string[] entries = Directory.GetFileSystemEntries(dir);
                foreach (string entry in entries)
                {
                    if (Directory.Exists(entry))
                    {
                        GetAllFiles(entry, files); // 递归子目录
                    }
                    else
                    {
                        files.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Access directory {dir} returned with error：{ex.Message}");
            }
        }
    }
}