namespace Utils
{
    public static class AngleUtils
    {
        public static float WrapAngle(float angle)
        {
            angle%=360;
            if(angle >180)
                return angle - 360;
 
            return angle;
        }
        
        public static float UnwrapAngle(float angle)
        {
            if(angle >=0)
                return angle;
 
            angle = -angle%360;
 
            return 360-angle;
        }
    }
}