
using System;
using System.Collections.Generic;
using Multiplayer.Entity.Server.Enemy;
using UnityEngine;

namespace ProceduralGeneration
{
    public class BlockPiece : MonoBehaviour
    {
        public static readonly List<BlockPiece> List = new ( 10 );
        
        [SerializeField] public PieceData pieceData;

        public PieceType currentType;

        public List<DoorEntrance> doors;

        public bool doorsInited = false;

        public int iteration = 0;

        [Header( "AI" )]
        public bool hasAI = false; 
        public Vector3 origin;
        public Vector3 size;

        public int aiCount = 5;

        public List<ServerEnemy.EnemyType> enemyTypes = new(2);

        private void Awake()
        {
            List.Add( this );
        }

        private void OnDestroy()
        {
            List.Remove( this );
        }

        public void Init( int it )
        {

            iteration = it;
        }

        public void InitDoors( DoorEntrance prefab )
        {
            doors = new List<DoorEntrance>();

            doorsInited = true;

            SpawnDoor( prefab, pieceData.forwardDoor );
            SpawnDoor( prefab, pieceData.backwardDoor );
            SpawnDoor( prefab, pieceData.rightDoor );
            SpawnDoor( prefab, pieceData.leftDoor );
        }

        private void SpawnDoor( DoorEntrance prefab, Transform trans )
        {
            if ( trans != null )
            {
                var door = Instantiate( prefab, trans.position, trans.rotation );
                
                door.transform.SetParent( transform );

                door.gameObject.SetActive( true );

                door.Init( this );
                
                doors.Add( door );
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
        }

        private void OnDrawGizmos()
        {
            Color color = Color.gray;
            
            switch ( currentType )
            {
                case PieceType.AI :
                    color = Color.yellow;
                    break;
                case PieceType.Loot:
                    color = Color.green;
                    break;
                case PieceType.Boss:
                    color = Color.red;
                    break;
            }

            color.a = 0.5f;

            Gizmos.color = color;

            Gizmos.DrawSphere( transform.position, 2.5f );

            if ( !doorsInited )
            {
                Gizmos.color = Color.black;

                DrawDoor( pieceData.forwardDoor );
                DrawDoor( pieceData.backwardDoor );
                DrawDoor( pieceData.rightDoor );
                DrawDoor( pieceData.leftDoor );
            }

            Gizmos.color = new Color( 1, 0, 0, 0.5f );

            Gizmos.matrix = Matrix4x4.TRS( transform.position, transform.rotation, transform.lossyScale );
            
            Gizmos.DrawCube( origin, size );
        }

        private void DrawDoor( Transform doorTrans )
        {
            if ( doorTrans != null )
            {
                Gizmos.matrix = Matrix4x4.TRS( doorTrans.position, doorTrans.rotation, doorTrans.lossyScale );
                Gizmos.DrawCube( new Vector3( 1.5f, 2, 0.25f ), new Vector3( 3, 4, 0.5f ) );
            }
        }
#endif
    }
}
