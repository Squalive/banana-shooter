using UnityEngine;

namespace Safe
{
    public class SafeInt
    {
        private int offset;
        private int value;
        public SafeInt (int value = 0) {
            offset = Random.Range(-1000, +1000);
            this.value = value + offset;
        }
    
        public int GetValue ()
        {
            return value - offset;
        }

        public int SetValue(int val)
        {
            value = val+offset;
            return GetValue();
        }
        public void Dispose ()
        {
            offset = 0;
            value = 0;
        }
        public override string ToString()
        {
            return GetValue().ToString();
        }
        public static SafeInt operator +(SafeInt f1, SafeInt f2) {
            return new SafeInt(f1.GetValue() + f2.GetValue());
        }
        public static SafeInt operator ++(SafeInt f1) {
            return new SafeInt(f1.GetValue()+1);
        }
        public static SafeInt operator --(SafeInt f1) {
            return new SafeInt(f1.GetValue()-1);
        }
        public static SafeInt operator *(SafeInt f1, SafeInt f2) {
            return new SafeInt(f1.GetValue() * f2.GetValue());
        }
    }
}