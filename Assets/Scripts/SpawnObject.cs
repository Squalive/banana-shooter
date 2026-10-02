

using Manager;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Server;
using Riptide;
using UnityEngine;

public class SpawnObject : MonoBehaviour
{
    public ObjectType type;

    public Transform[] spawnPos;

    
    public void Spawn()
    {
        if (type == ObjectType.SodaCan)
        {
            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.MMM_SODA);
        }
        Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.RequestSpawnObj);

        message.Add((int) type);
        Transform spawn = spawnPos[Random.Range(0, spawnPos.Length)];
        message.Add(spawn.position);
        message.Add(1);

        if (type == ObjectType.SodaCan)
        {
            message.Add(spawn.forward);
        }

        NetworkManager.Instance.SendByte += message.WrittenLength;
        
        NetworkManager.Instance.Client.Send(message);
    }
}
