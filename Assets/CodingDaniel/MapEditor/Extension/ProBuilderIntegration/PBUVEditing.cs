using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.ProBuilder;
namespace CodingDaniel.MapEditor.Extension.ProBuilderIntegration
{
    public static class PBUVEditing
    {
        private static MethodInfo _methodInfo;
        static PBUVEditing()
        {
            Type type = typeof(ProBuilderMesh).Assembly.GetType("UnityEngine.ProBuilder.MeshOperations.UVEditing");
            if (type == null)
            {
                Debug.LogWarning("AutoStitch is not supported");
                return;
            }

            _methodInfo = type.GetMethod("AutoStitch", BindingFlags.Public | BindingFlags.Static);
            if (_methodInfo == null)
            {
                Debug.LogWarning("AutoStitch method was not found");
                return;
            }
        }

        /// <summary>
        /// Provided two faces, this method will attempt to project @f2 and align its size, rotation, and position to match
        /// the shared edge on f1.  
        /// </summary>
        public static void AutoStitch(PBMesh mesh, int f1, int f2, int channel)
        {          
            if(_methodInfo == null)
            {
                return;
            }
           
            ProBuilderMesh pbMesh = mesh.ProBuilderMesh;
            IList<Face> faces = new List<Face>(2);
            pbMesh.GetFaces(new List<int> { f1, f2 }, faces);

            _methodInfo.Invoke(null, new object[] { pbMesh, faces[0], faces[1], channel });
        }
    }
}