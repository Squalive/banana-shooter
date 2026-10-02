using Cosmetic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class FindMissingFromItemDef
    {
        [MenuItem("Assets/Find Missing References From Item Def")]
        public static void Execute()
        {
            var guids = AssetDatabase.FindAssets("t:CosmeticItem");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<CosmeticItem>(path);
                if (item != null)
                {
                    if (item.icon == null)
                    {
                        Debug.LogError($"Path: {path}, icon is null");
                    }

                    foreach (var material in item.materials)
                    {
                        if (material == null)
                        {
                            Debug.LogError($"Path: {path}, material is null");
                            break;
                        }
                    }
                }
            }
        }
    }
}