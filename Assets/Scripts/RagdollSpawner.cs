using System.Collections;
using System.Collections.Generic;

using Multiplayer.Entity.Client;
using UnityEngine;

public class RagdollSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    
    
    public void Spawn()
    {
        if (PlayerState.PlayerStates.Count > 0)
        {
            PlayerState playerState = PlayerState.PlayerStates[Random.Range(0, PlayerState.PlayerStates.Count)];
            
            playerState.SpawnRagdoll(false,true).transform.position = spawnPoint.position;
        }
    }
}
