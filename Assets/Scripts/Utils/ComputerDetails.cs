using System;
using System.Text;
using Steamworks;
using UnityEngine;

namespace Utils
{
    public static class ComputerDetails
    {
        private static StringBuilder _details;

        public static void Initialize()
        {
            Refresh();
            
            Debug.Log(_details.ToString());
        }

        public static void Refresh()
        {
            _details = new StringBuilder();
            
            _details.Append($"System\n\tName: {SystemInfo.deviceName}\n\tOS: {SystemInfo.operatingSystem}\n");
            _details.Append($"CPU\n\tModel: {SystemInfo.processorType}\n\tCores: {SystemInfo.processorCount}\n\tMemory: {SystemInfo.systemMemorySize} MB\n");
            _details.Append($"GPU\n\tModel: {SystemInfo.graphicsDeviceName}\n\tAPI: {SystemInfo.graphicsDeviceVersion}\n\tMemory: {SystemInfo.graphicsMemorySize} MB\n\tSM: {SystemInfo.graphicsShaderLevel}\n");
            _details.Append($"Mono\n\tCollects: {GC.CollectionCount(0)}\n\tMemory: {(float)System.GC.GetTotalMemory(false) / (1024 * 1024)} MB\n");
            _details.Append($"Steam\n\tSteamID: {SteamFriends.GetPersonaName()} ({SteamUser.GetSteamID()})\n");
        }

        public static string GetDetails()
        {
            return _details.ToString();
        }
    }
}
