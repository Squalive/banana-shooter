using System.Collections.Generic;
using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.LagCompensation;
using UnityEngine;
using Weapon;

namespace Multiplayer.Entity.Interface
{
    public interface IPlayer : IEntity,IDamageable,IStats
    {
        #region Basic

        //PLayer's Steam Id
        ulong SteamId { get; set; }
        
        //Player's Name
        string Username { get; set; }

        //Is the player got eliminated (use for battle royale gamemode)
        bool Eliminated { set; get; }
        
        //
        bool DisplayTag { get;set; }
        
        List<Perk> Perks { get; set; }
        
        #endregion

        #region Statics
        
        //Player's current cash use for economy gamemode
        int Cash { get; set; }

        float StayTime { get; set; }

        short Coin { get; set; }
        #endregion

        #region State
        
        public Vector3 Velocity { set; get; }

        //Is the player spectating
        bool Specting { set; get; }
        
        //Use for checking player's infected state
        bool IsInfected { set; get; }
        
        //Use for checking player's infected state
        bool IsAiming { get; set; }

        #endregion

        int CurrentWeaponIndex { set; get; }
        short[] Weapons { get; }
        string Description { set; get; }
        ushort CurrentLifeKill { set; get; }
        ActiveWeapon[] ActiveWeapons { get; set; }
        int TacticalThrowCount { get; set; }
        int WeaponLevel { get; set; }
        bool IsCrazy { set; get; }
        Team Team { get; set; }

        #region Movement

        TransformUpdate[] TransformBuffer { get; }
        Transform PlayerTransform { set; get; }
        Rigidbody PlayerRb { get; set; }
        float XRotation { get; set; }
        bool Grounded { get; set; }
        bool Crouch { set; }
        Vector3 GrapplePoint { get; set; }
        bool IsGrappling { get; set; }

        #endregion
        
        bool HasPerk(Perk perk);
        Vector3 Position();
        string Nick();
    }
}