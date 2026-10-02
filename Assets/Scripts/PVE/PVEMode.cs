
using System.Collections;
using Multiplayer.Entity.Server.Enemy;
using ProceduralGeneration;
using UnityEngine;

namespace PVE
{
    public class PVEMode : GameModes
    {
        private WaitForSeconds _second;
        
        private void Awake()
        {
            leftTime = 400f;

            _second = new WaitForSeconds( 1f );
        }

        protected override void Init()
        {
            StartCoroutine( StartLoop() );
        }

        private IEnumerator StartLoop()
        {
            while ( !started )
            {
                yield return _second;
            }

            while ( GenerationTest.Instance.CurrentIteration > 0 )
            {
                while ( ServerEnemy.list.Count > 0 )
                {
                    yield return _second;
                }
                
                yield return Loop();
                
                GenerationTest.Instance.NewIteration();
            }
        }

        private IEnumerator Loop()
        {
            
            // while ( ServerEnemy.list.Count > 0 )
            // {
            //         
            // }
            yield return new WaitForSeconds( 8f );
        }
    }
}