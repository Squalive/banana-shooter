using System.Collections;
using Manager;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.Server;
using PlayerCameraController;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Spawn Command",menuName = "Utilities/DeveloperConsole/Commands/Spawn Command")]
    public class SpawnCommand : ConsoleCommand
    {
        public override bool Process(string[] args)
        {
            if (!RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID))
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : You dont have permission to do that</color>");
                return false;
            }
            if (!MoveCamera.Instance)
            {
                DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Not in game</color>");
                return false;
            }

            int spawnType = 0;
            if (args.Length!=0&&args.Length <= 2)
            {
                int type = (int)ServerEnemy.EnemyType.Jack;
                switch (args[0])
                {
                    case "jack":
                        type = (int)ServerEnemy.EnemyType.Jack;
                        break;
                    case "zombie":
                        type = (int)ServerEnemy.EnemyType.Zombie;
                        break;
                    case "turret":
                        type = (int)ServerEnemy.EnemyType.Turret;
                        break;
                    case "kat":
                        type = (int)ServerEnemy.EnemyType.Kat;
                        break;
                    case "package":
                        spawnType = 1;
                        type = (int)ObjectType.Package;
                        break;
                    case "crate":
                        spawnType = 1;
                        type = (int)ObjectType.Crate;
                        break;
                }

                Vector3 spawnPos;

                var transform = MoveCamera.Instance.transform;
                Vector3 ogPos = transform.position,dir = transform.forward;
                if (Physics.Raycast(ogPos,dir , out var hit))
                {
                    spawnPos = hit.point+Vector3.up*2f;
                }
                else
                {
                    if (Physics.Raycast(ogPos + dir * 30f, Vector3.down, out hit))
                    {
                        spawnPos = hit.point+Vector3.up*2f;
                    }
                    else
                    {
                        spawnPos = ogPos + dir * 30f;
                    }
                }
                Message message;

                if (args.Length == 2)
                {
                    
                    if (int.TryParse(args[1], out int times))
                    {
                        switch (spawnType)
                        {
                            case 0:
                                NetworkManager.Instance.StartCoroutine(Spawn(times, (ushort) type, spawnPos));
                                break;
                            case 1:
                                message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.RequestSpawnObj);
                                message.Add( type);
                                message.Add(spawnPos);
                                message.Add(times);

                                if ((ObjectType) type == ObjectType.SodaCan)
                                {
                                    message.Add(Vector3.up);
                                }
                
                                NetworkManager.Instance.SendByte += message.WrittenLength;
                                NetworkManager.Instance.Client.Send(message);
                                break;
                        }
                        
                    }
                    return true;
                
                }
                else
                {
                    switch (spawnType)
                    {
                        case 0:
                            message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.SpawnEnemy);
                            message.Add((ushort) type);
                            message.Add(spawnPos);

                            NetworkManager.Instance.SendByte += message.WrittenLength;
                            NetworkManager.Instance.Client.Send(message);
                            break;
                        case 1:
                            message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.RequestSpawnObj);
                            message.Add( type);
                            message.Add(spawnPos);
                            message.Add(1);
                            if ((ObjectType) type == ObjectType.SodaCan)
                            {
                                message.Add(Vector3.up);
                            }
                
                            NetworkManager.Instance.SendByte += message.WrittenLength;
                            NetworkManager.Instance.Client.Send(message);
                            break;
                    }
                    return true;
                }
            }
            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : arguments should be at least 1</color>");
            return false;
        }

        IEnumerator Spawn(int times,ushort type,Vector3 spawnPos)
        {
            for (int i = 0; i < times; i++)
            {
                Message message = Message.Create(MessageSendMode.Unreliable,(ushort) ClientToServerId.SpawnEnemy);
                message.Add( type);
                message.Add(spawnPos);

                spawnPos += Vector3.up*2f;
                
                NetworkManager.Instance.SendByte += message.WrittenLength;
                NetworkManager.Instance.Client.Send(message);
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}
