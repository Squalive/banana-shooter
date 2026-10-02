
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Riptide;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace ProceduralGeneration
{
    public class GenerationTest : MonoBehaviour
    {
        public static GenerationTest Instance { get; private set; }
        
        [SerializeField] private BlockPiece[ ] pieces;
        
        [SerializeField] private BlockPiece[ ] starterPieces;

        [SerializeField] private DoorEntrance doorPrefab;

        [SerializeField] private Vector3 offset = Vector3.zero;

        [SerializeField] public uint seed = 215;

        [Range(1, 60)] [SerializeField] private int iteration = 5;

        private int _currentIteration = 0;

        public int CurrentIteration => _currentIteration;

        private void Awake()
        {
            Instance = this;
        }

        // private void Update()
        // {
        //     if ( Input.GetKeyDown( KeyCode.V ) )
        //     {
        //         NewIteration();
        //     }
        //     if ( Input.GetKeyDown( KeyCode.F ) )
        //     {
        //         ResetIteration();
        //     }
        // }

        public void DestroyAll()
        {
            List<GameObject> toDestroy = new List<GameObject>();
            
            for ( int i = 0; i < transform.childCount; i++ )
            {
                toDestroy.Add( transform.GetChild( i ).gameObject );
            }

            for ( int i = 0; i < toDestroy.Count; i++ )
            {
                DestroyImmediate( toDestroy[ i ] );
            }
        }

        public void Generate( int it = 5 )
        {
            iteration = it;
            
            _currentIteration = iteration + 1;
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            DestroyAll();
            
            Generation.Initialize();
            
            for ( int i = 0; i < pieces.Length; i++ )
            {
                if ( i < starterPieces.Length )
                {
                    Generation.AddStarterPiece( starterPieces[ i ] );
                }
                else
                {
                    Generation.AddPieceData( pieces[ i ] );
                }
            }

            var results = Generation.Generate( seed, iteration );

            // return;
            var lastData = new Generation.Data
            {
                Piece = Spawn( results[ 0 ] ),
                Type = results[ 0 ].Type,
                XY = results[ 0 ].XY,
                Iteration = results[ 0 ].Iteration,
                HorizontalDirection = results[ 0 ].HorizontalDirection,
                VerticalDiretion = results[ 0 ].VerticalDiretion,
            };

            RecursiveGenerate( results, 1, lastData );

            Debug.Log( $"[{stopwatch.ElapsedMilliseconds / 1000f:F3}s] Level Generated" );
        }

        private void RecursiveGenerate( IReadOnlyList<Generation.Data> results, int startIndex, Generation.Data lastData )
        {
            for ( var i = startIndex; i < results.Count; i++ )
            {
                var result = results[ i ];

                if ( result.Iteration != lastData.Iteration - 1 )
                    continue;

                if ( !IsClose( result.XY, lastData.XY ) )
                    continue;

                Transform trans = null;

                if ( ( result.VerticalDiretion & GenerationDirection.Forward ) == GenerationDirection.Forward )
                {
                    trans = lastData.Piece.pieceData.forward;
                }
                else if ( ( result.VerticalDiretion & GenerationDirection.Backward ) == GenerationDirection.Backward )
                {
                    trans = lastData.Piece.pieceData.backward;
                }
                else if ( ( result.HorizontalDirection & GenerationDirection.Backward ) == GenerationDirection.Backward )
                {
                    trans = lastData.Piece.pieceData.right;
                }
                else if ( ( result.HorizontalDirection & GenerationDirection.Forward ) == GenerationDirection.Forward )
                {
                    trans = lastData.Piece.pieceData.left;
                }

                if ( trans != null )
                {
                    var obj = Spawn( result );
                    
                    obj.transform.position = trans.position;
                    obj.transform.rotation = trans.rotation;
                
                    var newData = new Generation.Data
                    {
                        Piece = obj,
                        Type = result.Type,
                        XY = result.XY,
                        Iteration = result.Iteration,
                        HorizontalDirection = result.HorizontalDirection,
                        VerticalDiretion = result.VerticalDiretion,
                    };

                    RecursiveGenerate( results, 0, newData );
                }
            }
        }

        private static bool IsClose( int2 x, int2 y )
        {
            return math.abs( x.x - y.x ) <= Generation.SearchLength && math.abs( x.y - y.y ) <= Generation.SearchLength;
        }

        private BlockPiece Spawn( Generation.Data data )
        {
            var piece = Instantiate( data.Piece, transform );

            piece.currentType = data.Type;

            piece.transform.position = offset;

            piece.Init( data.Iteration );
            piece.InitDoors( doorPrefab );

            return piece;
        }

        public void ResetIteration()
        {
            _currentIteration = iteration + 1;
            
            SendIteration();
            
            OpenDoors();
        }


        public void NewIteration()
        {
            _currentIteration --;
            
            SendIteration();
            
            OpenDoors();
            
            SpawnEnemies();
        }

        private void SendIteration()
        {
            if ( !NetworkServerManager.Instance.Server.IsRunning )
                return;
            
            Message message = Message.Create( MessageSendMode.Reliable, (ushort)ServerToClientId.PveIterate );

            message.AddInt( _currentIteration );

            NetworkServerManager.Instance.Server.SendToAll( message );
        }

        private void SpawnEnemies()
        {
            foreach ( var blockPiece in BlockPiece.List )
            {
                if ( !blockPiece.hasAI )
                    continue;
                
                if ( blockPiece.iteration + 1 != _currentIteration )
                    continue;

                var origin = blockPiece.transform.TransformPoint( blockPiece.origin );
                
                StartCoroutine( Wave( origin, blockPiece ) );
            }
        }

        private IEnumerator Wave( Vector3 origin, BlockPiece blockPiece )
        {
            var waves = math.clamp( ServerPlayer.list.Count / 2, 1, 4 );

            for ( int i = 0; i < waves; i++ )
            {
                for ( int j = 0; j < blockPiece.aiCount; j++ )
                {
                    var spawnPoint = GetSpawnPoint( origin, blockPiece.size );

                    while ( !Physics.Raycast(spawnPoint, Vector3.down, 100, 1 << 3) )
                    {
                        spawnPoint = GetSpawnPoint( origin, blockPiece.size );
                    }

                    ServerEnemy.SpawnEnemy( blockPiece.enemyTypes[ Random.Range( 0, blockPiece.enemyTypes.Count ) ], spawnPoint );
                }

                while ( ServerEnemy.list.Count > 2 )
                {
                    yield return null;
                }

                yield return new WaitForSeconds( 1 );
            }
            
            

        }

        private Vector3 GetSpawnPoint( Vector3 origin, Vector3 size )
        {
            return origin + new Vector3( Random.Range( -size.x / 2f, size.x / 2f ), size.y, Random.Range( -size.z / 2f, size.z / 2f ) );
        }

        private void OpenDoors()
        {
            foreach ( var door in DoorEntrance.List )
            {
                door.SetOpened( _currentIteration <= door.piece.iteration );
            }
        }

        [MessageHandler( (ushort)ServerToClientId.PveIterate )]
        private static void PveIterate( Message message )
        {
            if ( NetworkServerManager.Instance.Server.IsRunning )
                return;
            
            int iteration = message.GetInt();

            Instance._currentIteration = iteration;
            
            Instance.OpenDoors();
        }
    }
}