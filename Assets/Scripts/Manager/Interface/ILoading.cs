using Multiplayer.Interface;
using Steamworks;
using UnityEngine;

namespace Manager.Interface
{
    public interface ILoading
    {
        void ResetScene(bool isWorkshopMap=false);
        void LoadGame( string map, Texture2D texture2D, string otherText, params int[ ] external );
        void LoadWorkshopGame(PublishedFileId_t map,int external);
    }
}