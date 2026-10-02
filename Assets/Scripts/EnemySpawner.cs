

using Manager;
using Multiplayer;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.Server;
using Riptide;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform spawnPos;
    
    
    public void Click()
    {
        Message message = Message.Create(MessageSendMode.Reliable,(ushort) ClientToServerId.SpawnEnemy);

        message.Add((ushort) ServerEnemy.EnemyType.Jack);
        message.Add(spawnPos.position);
        NetworkManager.Instance.SendByte += message.WrittenLength;
        
        NetworkManager.Instance.Client.Send(message);
    }
}
