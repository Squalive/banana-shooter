using System;
using System.Collections;
using System.Collections.Generic;
using Manager;
using Multiplayer;
using Riptide;
using UnityEngine;

public class ClientFire : MonoBehaviour
{

    [MessageHandler((ushort) ServerToClientId.FireInit, NetworkManager.PlayerHostedDemoMessageHandlerGroupId)]
    private static void FireInit(Message message)
    {

        Vector3 pos = message.GetVector3();

        ClientFire fire = Instantiate(PrefabManager.Instance.GetPrefab("ClientFire"), pos, Quaternion.identity)
            .GetComponent<ClientFire>();
        
    }


}
