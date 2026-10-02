#if UNITY_EDITOR
//#define DEBUG_LOG
using System.Text;
using Debug = UnityEngine.Debug;
#endif

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace ProceduralGeneration
{
    [Flags]
    public enum GenerationDirection 
    {
        None = 0,
        Forward = 1 << 0,
        Backward = 1 << 1,
        Up = 1 << 2,
        Down = 1 << 3,
    }

    public enum Direction
    {
        Forward = 0,
        Backward,
        Left,
        Right,
    }

    [Flags]
    public enum PieceType
    {
        None = 0,
        Loot = 1 << 0,
        AI = 1 << 1,
        Boss = 1 << 2,
        
    }

    [Serializable]
    public class PieceData
    {
        public GenerationDirection horizontal, vertical;

        public PieceType possibleType;

        public Transform forward, backward, right, left;

        public Transform forwardDoor, backwardDoor, rightDoor, leftDoor;

        public int GetLength()
        {
            int length = 0;

            if ( forward != null )
                length ++;

            if ( backward != null )
                length ++;

            if ( right != null )
                length++;

            if ( left != null )
                length++;

            return length;
        }
    }
    
    public static class Generation
    {
        public const int SearchLength = 1;
        
        private static Dictionary<GenerationDirection, List<BlockPiece>> _horizontalPieces;
        private static Dictionary<GenerationDirection, List<BlockPiece>> _verticalPieces;

        private static Dictionary<int, List<BlockPiece>> _pieces;

        private static List<BlockPiece> _starterPieces;

        private static Random _random;
        
        private static int _maxIteration;

        private static int3 _middlePoint;

        private static PieceType[ ,, ] _types;

        public class Data
        {
            public BlockPiece Piece;

            public PieceType Type;

            public int2 XY;

            public int Iteration;

            public GenerationDirection HorizontalDirection, VerticalDiretion;
        }
        
        private class DataComparer : IComparer<Data>
        {
            public int Compare( Data x, Data y )
            {
                if ( y != null && x != null && x.Iteration > y.Iteration )
                    return -1;
                
                if ( y != null && x != null && x.Iteration < y.Iteration )
                    return 1;

                return 0;
            }
        }

        public static void Initialize()
        {
            _horizontalPieces = new Dictionary<GenerationDirection, List<BlockPiece>>();
            
            _verticalPieces = new Dictionary<GenerationDirection, List<BlockPiece>>();
            
            _pieces = new Dictionary<int, List<BlockPiece>>();
            
            _starterPieces = new List<BlockPiece>();
        }

        public static void AddPieceData( BlockPiece pieceData )
        {
            AddPieceDataTo( _horizontalPieces, pieceData, pieceData.pieceData.horizontal );
            AddPieceDataTo( _verticalPieces, pieceData, pieceData.pieceData.vertical );

            AddPieceDataTo( _pieces, pieceData, pieceData.pieceData.GetLength() );
        }

        public static void AddStarterPiece( BlockPiece pieceData )
        {
            _starterPieces.Add( pieceData );
        }

        private static void AddPieceDataTo<T>( IDictionary<T, List<BlockPiece>> dictionary, BlockPiece pieceData, T key )
        {
            if ( !dictionary.TryGetValue( key, out var datas ) )
            {
                datas = new List<BlockPiece>();

                dictionary.Add( key, datas );
            }

            datas.Add( pieceData );
        }

        public static List<Data> Generate( uint seed, int iteration = 5 )
        {
            if ( _pieces.Count <= 0 )
            {
#if DEBUG_LOG
                Debug.LogError( "failed to generate map, due to no valid pieces founded" );
#endif
                return null;
            }
            
            _random = new Random( seed );

            _random.NextBool();
            
            var starter = _starterPieces[ _random.NextInt( 0, _starterPieces.Count ) ];

            _maxIteration = iteration * 2;

            var occupied = new BlockPiece[ _maxIteration, _maxIteration, _maxIteration ];

            var iterations = new int[ _maxIteration, _maxIteration, _maxIteration ];
            
            var horizontalDirections = new GenerationDirection[ _maxIteration, _maxIteration, _maxIteration ];
            var verticalDirections = new GenerationDirection[ _maxIteration, _maxIteration, _maxIteration ];
            
            _types = new PieceType[ _maxIteration, _maxIteration, _maxIteration ];
            
            for ( int i = 0; i < _maxIteration; i++ )
            {
                for ( int j = 0; j < _maxIteration; j++ )
                {
                    for ( int k = 0; k < _maxIteration; k++ )
                    {
                        occupied[ i, j, k ] = null;
                        iterations[ i, j, k ] = 0;
                        horizontalDirections[ i, j, k ] = 0;
                        verticalDirections[ i, j, k ] = 0;
                        _types[ i, j, k ] = PieceType.None;
                    }
                }
            }
            
            _middlePoint = new int3( _maxIteration / 2, _maxIteration / 2, _maxIteration / 2 );

            iterations.Set( _middlePoint, iteration );
            occupied.Set( _middlePoint, starter );
            verticalDirections.Set( _middlePoint, GenerationDirection.None );
            horizontalDirections.Set( _middlePoint, GenerationDirection.None );
            _types.Set( _middlePoint, PieceType.None );
            
            --iteration;

            Find( occupied, iterations, horizontalDirections, verticalDirections, _middlePoint, Direction.Forward, iteration );

            List<Data> datas = new List<Data>();

#if DEBUG_LOG
            var sb = new StringBuilder();
            var sb1 = new StringBuilder();
#endif
            
            for ( int i = 0; i < _maxIteration; i++ )
            {
#if DEBUG_LOG
                sb.Clear();
                sb1.Clear();
#endif
                bool hasValue = false;
                
                for ( int j = 0; j < _maxIteration; j++ )
                {
                    for ( int k = 0; k < _maxIteration; k++ )
                    {
                        var it = iterations[ j, k, i ];
                        var occupy = occupied[ j, k, i ];
                        var horizontalDirection = horizontalDirections[ j, k, i ];
                        var verticalDirection = verticalDirections[ j, k, i ];
                        var type = _types[ j, k, i ];
#if DEBUG_LOG
                        sb.Append( $"{it}\t" );

                        if ( occupy == null )
                        {
                            sb1.Append( "0\t" );
                        }
                        else
                        {
                            sb1.Append( $"{occupy.name}\t" );

                            datas.Add( new Data
                            {
                                Iteration = it,
                                XY = new int2( j, k ),
                                Type = type,
                                Piece = occupy,
                                HorizontalDirection = horizontalDirection,
                                VerticalDiretion = verticalDirection,
                            } );
                            
                            hasValue = true;
                        }
#else
                        if ( occupy != null )
                        {
                            datas.Add( new Data
                            {
                                Iteration = it,
                                XY = new int2( j, k ),
                                Type = type,
                                Piece = occupy,
                                HorizontalDirection = horizontalDirection,
                                VerticalDiretion = verticalDirection,
                            } );
                            
                            hasValue = true;
                        }
#endif
                    }
#if DEBUG_LOG
                    sb.AppendLine();
                    sb1.AppendLine();
#endif
                }

                if ( hasValue )
                {
#if DEBUG_LOG
                    Debug.Log(sb.ToString());
                    Debug.Log(sb1.ToString());
#endif
                }
            }

            datas.Sort( new DataComparer() );
            
            // 
            
            return datas;
        }

        private static bool Find( BlockPiece[ ,, ] occupied, int[,,] iterations, GenerationDirection[,,] horizontalDirections, GenerationDirection[,,] verticalDirections, int3 current, Direction currentDirection, int iteration )
        {
            if ( iteration <= 0 )
                return true;
            
            var currentPieceData = occupied.Get( current );

            var nextPoint = current;
            GenerationDirection direction = GenerationDirection.None;
            Direction nextDirection = currentDirection;

            if ( currentPieceData.pieceData.vertical == GenerationDirection.Forward )
            {
                nextPoint = current + TransformOffset( new int3( 0, SearchLength, 0 ), currentDirection );
                direction = GenerationDirection.Forward;
            }
            else if ( ( currentPieceData.pieceData.vertical & (GenerationDirection.Forward | GenerationDirection.Up) ) == ( GenerationDirection.Forward | GenerationDirection.Up ) )
            {
                nextPoint = current + TransformOffset( new int3( 0, SearchLength, SearchLength ), currentDirection );
                direction = ( GenerationDirection.Forward | GenerationDirection.Up );
            }
            else if ( ( currentPieceData.pieceData.vertical & ( GenerationDirection.Forward | GenerationDirection.Down ) ) == ( GenerationDirection.Forward | GenerationDirection.Down ) )
            {
                nextPoint = current + TransformOffset( new int3( 0, SearchLength, -SearchLength ), currentDirection );
                direction = ( GenerationDirection.Forward | GenerationDirection.Down );
            }

            var tryFind = TryFind( verticalDirections );

            if ( tryFind != -1 )
            {
                return tryFind == 1;
            }

            if ( currentPieceData.pieceData.vertical == GenerationDirection.Backward )
            {
                nextPoint = current + TransformOffset( new int3( 0, -SearchLength, 0 ), currentDirection );
                direction = GenerationDirection.Backward;

                nextDirection = nextDirection.TransformDirection( Direction.Backward );
            }
            else if ( ( currentPieceData.pieceData.vertical & (GenerationDirection.Backward | GenerationDirection.Up) ) == ( GenerationDirection.Backward | GenerationDirection.Up ) )
            {
                nextPoint = current + TransformOffset( new int3( 0, -SearchLength, SearchLength ), currentDirection );
                direction = ( GenerationDirection.Backward | GenerationDirection.Up );

                nextDirection = nextDirection.TransformDirection( Direction.Backward );
            }
            else if ( ( currentPieceData.pieceData.vertical & (GenerationDirection.Backward | GenerationDirection.Down) ) == ( GenerationDirection.Backward | GenerationDirection.Down ) )
            {
                nextPoint = current + TransformOffset( new int3( 0, -SearchLength, -SearchLength ), currentDirection );
                direction = ( GenerationDirection.Backward | GenerationDirection.Down );

                nextDirection = nextDirection.TransformDirection( Direction.Backward );
            }

            tryFind = TryFind( verticalDirections );

            if ( tryFind != -1 )
            {
                return tryFind == 1;
            }
            
            nextPoint = current;
            direction = GenerationDirection.None;
            nextDirection = currentDirection;
            
            if ( (currentPieceData.pieceData.horizontal & GenerationDirection.Forward) == GenerationDirection.Forward )
            {
                nextPoint = current + TransformOffset( new int3( -SearchLength, 0, 0 ), currentDirection );
                direction = GenerationDirection.Forward;

                nextDirection = nextDirection.TransformDirection( Direction.Left );
            }
            else if ( (currentPieceData.pieceData.horizontal & ( GenerationDirection.Forward | GenerationDirection.Up )) == ( GenerationDirection.Forward | GenerationDirection.Up ) )
            {
                nextPoint = current + TransformOffset( new int3( -SearchLength, 0, SearchLength ), currentDirection );
                direction = ( GenerationDirection.Forward | GenerationDirection.Up );

                nextDirection = nextDirection.TransformDirection( Direction.Left );
            }
            else if ( (currentPieceData.pieceData.horizontal & ( GenerationDirection.Forward | GenerationDirection.Down )) == ( GenerationDirection.Forward | GenerationDirection.Down ) )
            {
                nextPoint = current + TransformOffset( new int3( -SearchLength, 0, -SearchLength ), currentDirection );
                direction = ( GenerationDirection.Forward | GenerationDirection.Down );

                nextDirection = nextDirection.TransformDirection( Direction.Left );
            }
            
            tryFind = TryFind( horizontalDirections );

            if ( tryFind != -1 )
            {
                return tryFind == 1;
            }
            
            nextPoint = current;
            direction = GenerationDirection.None;
            nextDirection = currentDirection;
            
            if ( (currentPieceData.pieceData.horizontal & ( GenerationDirection.Backward)) == GenerationDirection.Backward )
            {
                nextPoint = current + TransformOffset( new int3( SearchLength, 0, 0 ), currentDirection );
                direction = ( GenerationDirection.Backward );

                nextDirection = nextDirection.TransformDirection( Direction.Right );
            }
            else if ( (currentPieceData.pieceData.horizontal & ( GenerationDirection.Backward | GenerationDirection.Up)) == ( GenerationDirection.Backward | GenerationDirection.Up ) )
            {
                nextPoint = current + TransformOffset( new int3( SearchLength, 0, SearchLength ), currentDirection );
                direction = ( GenerationDirection.Backward | GenerationDirection.Up );

                nextDirection = nextDirection.TransformDirection( Direction.Right );
            }
            else if ( (currentPieceData.pieceData.horizontal & ( GenerationDirection.Backward | GenerationDirection.Down)) == ( GenerationDirection.Backward | GenerationDirection.Down ) )
            {
                nextPoint = current + TransformOffset( new int3( SearchLength, 0, -SearchLength ), currentDirection );
                direction = ( GenerationDirection.Backward | GenerationDirection.Down );

                nextDirection = nextDirection.TransformDirection( Direction.Right );
            }
            
            tryFind = TryFind( horizontalDirections );

            if ( tryFind != -1 )
            {
                return tryFind == 1;
            }

            return true;

            int TryFind( GenerationDirection[ ,, ] directions )
            {
                if ( !nextPoint.Equals( current ) )
                {
                    if ( !MaxOut( nextPoint ) && occupied.Get( nextPoint ) == null )
                    {
                        if ( !FindData( occupied, iterations, horizontalDirections, verticalDirections, directions, direction, nextDirection, nextPoint, iteration ) )
                        {
                            if ( current.Equals( _middlePoint ) )
                                return 1;

                            if ( iterations.Get( current ) <= iteration )
                            {
                                occupied.Set( current, null );
                                iterations.Set( current, 0 );
                                directions.Set( current, GenerationDirection.None );
                                _types.Set( current, PieceType.None );
                            }

                            return 0;
                        }
                    }
                }
                
                return -1;
            }
        }

        private static int3 TransformOffset( int3 offset, Direction currentDirection )
        {
            switch ( currentDirection )
            {
                case Direction.Backward:
                    offset = new int3( -offset.x, -offset.y, offset.z );
                    break;
                case Direction.Left:
                    // 1, 2 ======> -2, 1
                    offset = new int3( -offset.y, offset.x, offset.z );
                    break;
                case Direction.Right:
                    // -1, 0 ======> 0, 1
                    offset = new int3( offset.y, -offset.x, offset.z );
                    break;
            }

            return offset;
        }

        private static Direction TransformDirection( this Direction direction, Direction transformDirection )
        {
            switch ( direction )
            {
                case Direction.Forward:
                    return transformDirection;
                
                case Direction.Backward:
                    switch ( transformDirection )
                    {
                        case Direction.Backward:
                            return Direction.Forward;
                        case Direction.Left:
                            return Direction.Right;
                        case Direction.Right:
                            return Direction.Left;
                        
                        default: return direction;
                    }
                
                case Direction.Right:
                    switch ( transformDirection )
                    {
                        case Direction.Backward:
                            return Direction.Left;
                        case Direction.Left:
                            return Direction.Forward;
                        case Direction.Right:
                            return Direction.Backward;
                        
                        default: return direction;
                    }
                
                case Direction.Left:
                    switch ( transformDirection )
                    {
                        case Direction.Backward:
                            return Direction.Right;
                        case Direction.Left:
                            return Direction.Backward;
                        case Direction.Right:
                            return Direction.Forward;
                        
                        default: return direction;
                    }
            }

            return direction;
        }

        private static bool FindData( BlockPiece[ ,, ] occupied, int[ ,, ] iterations, GenerationDirection[,,] horizontalDirections, GenerationDirection[,,] verticalDirections, GenerationDirection[,,] directions, 
            GenerationDirection direction, Direction currentDirection, int3 pos, int iteration )
        {
            int channel = 0;

            float alpha = iteration / ( _maxIteration / 2f );

            if ( iteration != 1 )
                channel = GetDesiredChannel( alpha );

            int times;
            
            if ( !_pieces.TryGetValue( channel, out var datas ) )
            {
                times = 0;
                
                while ( !_pieces.TryGetValue( channel, out datas ) )
                {
                    times++;
                    
                    channel = GetDesiredChannel( alpha );

                    if ( times >= 100 )
                    {
                        return false;
                    }
                }
            }

            const int nextSearch = 1;
            
            var data = datas[ _random.NextInt( 0, datas.Count ) ];

            if ( !CheckNext() )
            {
                occupied.Set( pos, _pieces[ 0 ][ _random.NextInt( 0, _pieces[ 0 ].Count ) ] );
                iterations.Set( pos, iteration );
                directions.Set( pos, direction );
                
                _types.Set( pos, GetType() );
                
                Find( occupied, iterations, horizontalDirections, verticalDirections, pos, currentDirection, iteration - nextSearch );
                return true;
            }

            occupied.Set( pos, data );
            iterations.Set( pos, iteration );
            directions.Set( pos, direction );
                
            _types.Set( pos, GetType() );

            bool flag = Find( occupied, iterations, horizontalDirections, verticalDirections, pos, currentDirection, iteration - nextSearch );

            times = 0;
            
            while ( !flag )
            {
                data = datas[ _random.NextInt( 0, datas.Count ) ];
                
                occupied.Set( pos, data );
                iterations.Set( pos, iteration );
                directions.Set( pos, direction );
                
                _types.Set( pos, GetType() );
                
                flag = Find( occupied, iterations, horizontalDirections, verticalDirections, pos, currentDirection, iteration - nextSearch );

                times++;

                if ( times >= datas.Count )
                {
                    return false;
                }
            }

            return true;

            PieceType GetType()
            {
                PieceType type = RandomType();

                var possibleType = occupied.Get( pos ).pieceData.possibleType;

                int t = 0 ;

                while ( ( possibleType & type ) == 0 )
                {
                    t++;
                    
                    type =  RandomType();

                    if ( t >= 15 )
                        return possibleType;
                }

                return type;
            }

            PieceType RandomType()
            {
                int t = _random.NextInt( 0, 4 );

                switch ( t )
                {
                    case 1:
                        return PieceType.Loot;
                    case 2:
                        return _random.NextFloat() < 0.6f ? PieceType.Boss : PieceType.AI;
                    case 3:
                        return PieceType.Boss;
                    default:
                        return PieceType.None;
                }
            }

            bool CheckNext()
            {
                if ( ( data.pieceData.vertical & GenerationDirection.Forward ) == GenerationDirection.Forward )
                {
                    int3 offset = new int3( 0, 1, 0 );

                    if ( ( data.pieceData.vertical & GenerationDirection.Down ) == GenerationDirection.Down )
                    {
                        offset.z = -1;
                    }

                    else if ( ( data.pieceData.vertical & GenerationDirection.Up ) == GenerationDirection.Up )
                    {
                        offset.z = 1;
                    }
                    
                    var nextPos = pos + TransformOffset( offset, currentDirection );

                    if ( MaxOut( nextPos ) || occupied.Get( nextPos ) != null )
                        return false;
                }

                if ( ( data.pieceData.horizontal & GenerationDirection.Forward ) == GenerationDirection.Forward )
                {
                    var nextPos = pos + TransformOffset( new int3(-1, 0, 0), currentDirection );

                    if ( MaxOut( nextPos ) || occupied.Get( nextPos ) != null )
                        return false;
                }

                if ( ( data.pieceData.horizontal & GenerationDirection.Backward ) == GenerationDirection.Backward )
                {
                    var nextPos = pos + TransformOffset( new int3(1, 0, 0), currentDirection );
                    ;
                    if ( MaxOut( nextPos ) || occupied.Get( nextPos ) != null )
                        return false;
                }

                return true;
            }
        }

        private static int GetDesiredChannel( float alpha )
        {
            if ( alpha <= 0.6f )
            {
                return _random.NextInt( 1, 3 );
            }
            
            return _random.NextInt( 1, 4 );
        }

        private static void Set<T>( this T[ , , ] data, int3 pos, T value )
        {
            data[ pos.x , pos.y , pos.z ] = value;
        }

        private static T Get<T>( this T[ , , ] data, int3 value )
        {
            return data[ value.x , value.y , value.z ];
        }

        private static bool MaxOut( int3 pos )
        {
            if ( _maxIteration <= pos.x || _maxIteration <= pos.y || _maxIteration <= pos.z )
            {
                return true;
            }

            if ( pos.x < 0 || pos.y < 0 || pos.z < 0 )
            {
                return true;
            }
            
            return false;
        }
    }
}