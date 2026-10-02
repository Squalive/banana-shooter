using Steamworks;
using UnityEngine;

namespace Utils
{
    public static class SteamTextureUtils
    {
        public static Texture2D GetSteamImageAsTexture(int iImage)
        {
            Texture2D texture2D = null;
            bool isValid = SteamUtils.GetImageSize(iImage, out uint width, out uint height);
            if (isValid)
            {
                byte[] image = new byte[width * height * 4];

                isValid = SteamUtils.GetImageRGBA(iImage, image, (int) (width * height * 4));

                if (isValid)
                {
                    texture2D = new Texture2D((int) width, (int) height, TextureFormat.RGBA32, false);
                    texture2D.LoadRawTextureData(image);
                    texture2D.Apply();
                }
            }

            return texture2D;
        }
    }
}