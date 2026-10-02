#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ProceduralGeneration.Editor
{
    [CustomEditor(typeof(GenerationTest))]
    public class GenerationTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if ( GUILayout.Button( "Random ID" ) )
            {
                var random = new Random( (uint)( DateTime.Now.Ticks % uint.MaxValue ) );
                
                var test = (GenerationTest)target;

                test.seed = random.NextUInt();
            }

            if ( GUILayout.Button( "DestroyAll" ) )
            {
                var test = (GenerationTest)target;
                
                test.DestroyAll();
            }

            if ( GUILayout.Button( "Generate" ) )
            {
                var test = (GenerationTest)target;
                
                test.Generate();
            }

            if ( GUILayout.Button( "Random Generate" ) )
            {
                var test = (GenerationTest)target;
                
                var random = new Random( (uint)( DateTime.Now.Ticks % uint.MaxValue ) );

                test.seed = random.NextUInt();
                
                test.Generate();
            }

            if ( GUILayout.Button( "Next Iteration" ) )
            {
                var test = (GenerationTest)target;
                
                test.NewIteration();
            }

            if ( GUILayout.Button( "Reset Iteration" ) )
            {
                var test = (GenerationTest)target;
                
                test.ResetIteration();
            }
        }
    }
}
#endif
