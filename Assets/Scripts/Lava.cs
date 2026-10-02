
using System;
using Multiplayer;
using Multiplayer.Entity.Server;
using Multiplayer.Interface;
using Multiplayer.Server;
using UnityEngine;

public class Lava : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("ServerPlayer"))
        {
            ServerPlayer player = other.transform.root.GetComponent<ServerPlayer>();
            player.TakeDamage(200,42,NetworkServerManager.Instance.CurrentTick,false,false,1004);
        }
    }
}
