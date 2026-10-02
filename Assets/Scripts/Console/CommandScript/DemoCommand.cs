using System.Collections.Generic;
using System.Text;
using Demo;
using Demo.UI;
using Manager;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using Multiplayer.Interface;
using Riptide;
using UnityEngine;

namespace Console.CommandScript
{
    [CreateAssetMenu(fileName = "Demo Command",menuName = "Utilities/DeveloperConsole/Commands/Demo Command")]
    public class DemoCommand : ConsoleCommand
    {
        private DeveloperConsoleUI console;
        private DeveloperConsoleUI Console
        {
            get
            {
                if (console == null) return console = DeveloperConsoleUI.Instance;
                return console;
            }
        }

        private readonly HashSet<string> _stringVarFunction = new () {"save_point", "load_point", "record", "set_bone"};

        public override bool Process(string[] args)
        {

            if (args.Length > 1)
            {
                if (_stringVarFunction.Contains(args[0]))
                {
                    switch (args[0])
                    {
                        case "save_point":
                            SpectateMovement.Instance.AddNewSavePoint(args[1]);
                            return true;
                        case "load_point":
                            SpectateMovement.Instance.LoadSavePoint(args[1]);
                            return true;
                        case "record":
                            if (args.Length >= 2)
                            {
                                StringBuilder recordName = new StringBuilder(args[1]);
                                for (int i = 2; i < args.Length; i++)
                                {
                                    recordName.Append(' ');
                                    recordName.Append(args[i]);
                                }
                                DemoManager.Instance.StartRecord(recordName.ToString());
                            }
                            return true;
                        case "set_bone":
                            SpectateMovement.Instance.SetBone(args[1]);
                            return true;
                    }
                }
                else if (float.TryParse(args[1],out var fValue))
                {
                    switch (args[0])
                    {
                        case "cammovespeed":
                            SpectateMovement.Instance.moveSpeedMultiplier = fValue;
                            return true;
                        case "camlerp":                            
                            SpectateMovement.Instance.lerpAmount = fValue;
                            return true;
                        case "camzoomspeed":                            
                            SpectateMovement.Instance.zoomSpeedMultiplier = fValue;
                            return true;
                        case "camzoomlerp":                            
                            SpectateMovement.Instance.zoomLerpAmount = fValue;
                            return true;
                        case "timescale":
                            if (DemoManager.Replaying)
                            {
                                Time.timeScale = fValue;
                                
                                DemoCanvas.Instance.timeScaleText.SetText(fValue.ToString("F1"));
                            }
                            return true;
                    }
                }
                else if (bool.TryParse(args[1], out var flag))
                {
                    switch (args[0])
                    {
                        case "camlock":
                            SpectateMovement.Instance.Locked = flag;
                            return true;
                        case "playername":
                            PlayerState.DisplayPlayerName = flag;
                            return true;
                        case "bonerotation":
                            SpectateMovement.Instance.SetTargetRotationSync(flag);
                            return true;
                    }
                }
            }
            else
            {
                switch (args[0])
                {
                    case "camera":
                        if (!NetworkManager.Instance.Client.IsConnected) return false;
                        
                        if (!NetworkServerManager.Instance.Server.IsRunning)
                        {
                            if (!RolesManager.Instance.CheckIsAdmin(NetworkManager.Instance.steamId.m_SteamID))
                            {
                                DeveloperConsoleUI.Instance.AddMessageToConsole(
                                    "<color=yellow>Console : Server is not running or you are not an admin/color>");
                                return false;
                            }
                        }

                        Message message = Message.Create(MessageSendMode.Reliable,
                            (ushort)ClientToServerId.SpecterMode);

                        message.Add(!SpectateMovement.Instance.Spectating);
                        NetworkManager.Instance.SendByte += message.WrittenLength;

                        NetworkManager.Instance.Client.Send(message);
                        return true;
                    case "stoprecord":
                        DemoManager.Instance.StopRecord(true);
                        return true;
                    case "targetbind":
                        SpectateMovement.Instance.TryToBindTarget();
                        return true;
                    case "bonecycle":
                        SpectateMovement.Instance.CycleBone();
                        return true;
                    case "bonels":
                        SpectateMovement.Instance.BoneLs();
                        return true;
                }
            }

            DeveloperConsoleUI.Instance.AddMessageToConsole("<color=yellow>Console : Command not found</color>");
            return false;
        }
    }
}
