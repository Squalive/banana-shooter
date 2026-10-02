
using UnityEditor;
using UnityEngine;

namespace Editor
{

    public class ProBuilderMeshFixer
    {
        [MenuItem("ProBuilderHelper/Check and fix mesh UVs")]
        private static void CheckAndFixMesh()
        {
            if (Selection.activeGameObject == null) return;

            MeshFilter mf = Selection.activeGameObject.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            Vector2[] uvs = mf.sharedMesh.uv;
            bool assignBack = false;

            for (int i = 0; i < uvs.Length; i++)
            {
                if (float.IsNaN(uvs[i].x) || float.IsNaN(uvs[i].y))
                {
                    assignBack = true;
                    Debug.LogError($"Float error at index {i}: {uvs[i].ToString("F4")}");
                    uvs[i] = Vector2.zero;
                }
            }

            if (assignBack)
            {
                mf.sharedMesh.uv = uvs;
                Debug.Log("Mesh UVs fixed!");
            }
            else
            {
                Debug.Log("Mesh UVs are OK.");
            }
        }
    }
}