using System.Text;

namespace Utils
{
    public static class FnvConstants
    {
        public static readonly uint Basis = 2166136261u;
        public static readonly uint Prime = 16777619u;

        public static uint CreateHash(string txt)
        {
            uint ret = Basis;

            byte[] bytes = Encoding.UTF8.GetBytes(txt);
            for (int i = 0; i < txt.Length; i++)
            {
                ret ^= bytes[i];
                ret *= Prime;
            }

            return ret;
        }
    }
}