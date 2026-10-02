using System.Linq;
using UnityEngine;

namespace CodingDaniel.MapEditor.Interaction
{
    public class LockObject
    {
        private bool _positionX;
        private bool _positionY;
        private bool _positionZ;
        private bool _rotationX;
        private bool _rotationY;
        private bool _rotationZ;
        private bool _rotationFree;
        private bool _rotationScreen;
        private bool _scaleX;
        private bool _scaleY;
        private bool _scaleZ;
        private bool _rectXY;
        private bool _rectYZ;
        private bool _rectXZ;

        public bool PositionX { get { return _positionX || (_globalLock != null ? _globalLock._positionX : false); } set { _positionX = value; } }
        public bool PositionY { get { return _positionY || (_globalLock != null ? _globalLock._positionY : false); } set { _positionY = value; } }
        public bool PositionZ { get { return _positionZ || (_globalLock != null ? _globalLock._positionZ : false); } set { _positionZ = value; } }
        public bool RotationX { get { return _rotationX || (_globalLock != null ? _globalLock._rotationX : false); } set { _rotationX = value; } }
        public bool RotationY { get { return _rotationY || (_globalLock != null ? _globalLock._rotationY : false); } set { _rotationY = value; } }
        public bool RotationZ { get { return _rotationZ || (_globalLock != null ? _globalLock._rotationZ : false); } set { _rotationZ = value; } }
        public bool RotationFree { get { return _rotationFree || (_globalLock != null ? _globalLock._rotationFree : false); } set { _rotationFree = value; } }
        public bool RotationScreen { get { return _rotationScreen || (_globalLock != null ? _globalLock._rotationScreen : false); } set { _rotationScreen = value; } }
        public bool ScaleX { get { return _scaleX || (_globalLock != null ? _globalLock._scaleX : false); } set { _scaleX = value; } }
        public bool ScaleY { get { return _scaleY || (_globalLock != null ? _globalLock._scaleY : false); } set { _scaleY = value; } }
        public bool ScaleZ { get { return _scaleZ || (_globalLock != null ? _globalLock._scaleZ : false); } set { _scaleZ = value; } }
        public bool RectXY { get { return _rectXY || (_globalLock != null ? _globalLock._rectXY : false); } set { _rectXY = value; } }
        public bool RectYZ { get { return _rectYZ || (_globalLock != null ? _globalLock._rectYZ : false); } set { _rectYZ = value; } }
        public bool RectXZ { get { return _rectXZ || (_globalLock != null ? _globalLock._rectXZ : false); } set { _rectXZ = value; } }

        public MEPivotMode? PivotMode { get; set; }
        public MEPivotRotation? PivotRotation { get; set; }

        public bool IsPositionLocked
        {
            get { return PositionX && PositionY && PositionZ; }
        }

        public bool IsRotationLocked
        {
            get { return RotationX && RotationY && RotationZ && RotationFree && RotationScreen; }
        }

        public bool IsScaleLocked
        {
            get { return ScaleX && ScaleY && ScaleZ; }
        }

        public bool IsRectLocked
        {
            get { return RectXY && RectYZ && RectXZ; }
        }

        private LockObject _globalLock;
        public void SetGlobalLock(LockObject gLock)
        {
            _globalLock = gLock;
        }

        public LockObject()
        {
        }

        public LockObject(LockObject obj)
        {
            _positionX = obj._positionX;
            _positionY = obj._positionY;
            _positionZ = obj._positionZ;
            _rotationX = obj._rotationX;
            _rotationY = obj._rotationY;
            _rotationZ = obj._rotationZ;
            _rotationFree = obj._rotationFree;
            _rotationScreen = obj._rotationScreen;
            _scaleX = obj._scaleX;
            _scaleY = obj._scaleY;
            _scaleZ = obj._scaleZ;
            _rectXY = obj._rectXY;
            _rectYZ = obj._rectYZ;
            _rectXZ = obj._rectXZ;
            _globalLock = obj._globalLock;
        }

        public void Reset()
        {
            _positionX = false;
            _positionY = false;
            _positionZ = false;
            _rotationX = false;
            _rotationY = false;
            _rotationZ = false;
            _rotationFree = false;
            _rotationScreen = false;
            _scaleX = false;
            _scaleY = false;
            _scaleZ = false;
            _rectXY = false;
            _rectYZ = false;
            _rectXZ = false;
            _globalLock = null;
        }
    }

    public class LockAxes : MonoBehaviour
    {
        public bool PositionX;
        public bool PositionY;
        public bool PositionZ;
        public bool RotationX;
        public bool RotationY;
        public bool RotationZ;
        public bool RotationFree;
        public bool RotationScreen;
        public bool ScaleX;
        public bool ScaleY;
        public bool ScaleZ;
        public bool RectXY;
        public bool RectYZ;
        public bool RectXZ;

        public bool PivotMode;
        public MEPivotMode PivotModeValue;
        public bool PivotRotation;
        public MEPivotRotation PivotRotationValue;

        public void Reset()
        {
            PositionX = PositionY = PositionZ = false;
            RotationX = RotationY = RotationZ = RotationFree = RotationScreen =  false;
            ScaleX = ScaleY = ScaleZ = false;
            RectXY = RectXZ = RectYZ = false;
            PivotMode = false;
            PivotModeValue = MEPivotMode.Center;
            PivotRotation = false;
            PivotRotationValue = MEPivotRotation.Local;
        }

        public static LockObject Eval(LockAxes[] lockAxes)
        {
            LockObject lockObject = new LockObject();
            if(lockAxes != null)
            {
                lockObject.PositionX = lockAxes.Any(la => la.PositionX);
                lockObject.PositionY = lockAxes.Any(la => la.PositionY);
                lockObject.PositionZ = lockAxes.Any(la => la.PositionZ);

                lockObject.RotationX = lockAxes.Any(la => la.RotationX);
                lockObject.RotationY = lockAxes.Any(la => la.RotationY);
                lockObject.RotationZ = lockAxes.Any(la => la.RotationZ);
                lockObject.RotationFree = lockAxes.Any(la => la.RotationFree);
                lockObject.RotationScreen = lockAxes.Any(la => la.RotationScreen);

                lockObject.ScaleX = lockAxes.Any(la => la.ScaleX);
                lockObject.ScaleY = lockAxes.Any(la => la.ScaleY);
                lockObject.ScaleZ = lockAxes.Any(la => la.ScaleZ);

                lockObject.RectXY = lockAxes.Any(la => la.RectXY);
                lockObject.RectYZ = lockAxes.Any(la => la.RectYZ);
                lockObject.RectXZ = lockAxes.Any(la => la.RectXZ);

                lockObject.PivotMode = null;
                if(lockAxes.Any(la => la.PivotMode))
                {
                    if(lockAxes.All(la => la.PivotModeValue == MEPivotMode.Center))
                    {
                        lockObject.PivotMode =MEPivotMode.Center;
                    }
                    else if(lockAxes.All(la => la.PivotModeValue == MEPivotMode.Pivot))
                    {
                        lockObject.PivotMode = MEPivotMode.Pivot;
                    }
                }

                lockObject.PivotRotation = null;
                if(lockAxes.Any(la => la.PivotRotation))
                {
                    if (lockAxes.All(la => la.PivotRotationValue == MEPivotRotation.Global))
                    {
                        lockObject.PivotRotation = MEPivotRotation.Global;
                    }
                    else if (lockAxes.All(la => la.PivotRotationValue == MEPivotRotation.Local))
                    {
                        lockObject.PivotRotation = MEPivotRotation.Local;
                    }
                }
            }

            return lockObject;
        }
    }

}
