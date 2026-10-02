using System;
using System.Collections.Generic;
using Audio;
using EZCameraShake;
using UnityEngine;

namespace ProceduralGeneration
{
    public class DoorEntrance : MonoBehaviour
    {
        public static List<DoorEntrance> List = new ();

        public BlockPiece piece;
        
        public bool Opened { get; private set; } = false;

        private Vector3 _defaultPos, _openedPos;

        private float _time = 0 ;

        private void Awake()
        {
            List.Add( this );
        }

        private void OnDestroy()
        {
            List.Remove( this );
        }

        private void Start()
        {
            _defaultPos = transform.position;

            _openedPos = _defaultPos + new Vector3( 0, 3.8f, 0 );
        }

        private void Update()
        {
            transform.position = Vector3.Lerp( transform.position, Opened ? _openedPos : _defaultPos, _time );

            _time += Time.deltaTime * 0.0025f;
        }

        public void Init( BlockPiece blockPiece )
        {
            piece = blockPiece;
        }

        public void SetOpened( bool opened )
        {
            if ( !Opened && opened )
            {
                CameraShaker.Instance.Shake( CameraShakePresets.EarthquakeShort );

                AudioManager.Instance.Play( "doors_open" );
            }
            
            Opened = opened;

            _time = 0;
        }
    }
}