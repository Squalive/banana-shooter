using Manager;
using Multiplayer.Entity.Server;
using Multiplayer.Entity.Server.Enemy;
using Multiplayer.Server;
using UnityEngine;

namespace Multiplayer.LagCompensation
{
    public class LagCompensationHitbox : MonoBehaviour
    {
        public enum HitboxType
        {
            Player,
            Jack,
            Zombie,
            Turret,
            Kat,
        }
        public ushort Id { private set; get; } = 0;

        uint Tick { set; get; } = 0;

        public HitboxType type = HitboxType.Player;

        public void Initialize(ushort id,uint tick)
        {
            Id = id;
            Tick = tick;
        }

        public void TakeDamage(ushort fromClient,uint tick, bool wall,bool isAiming,HitboxType t,Vector3 hitPoint,bool headShot=false, short desiredWeaponIndex=-1)
        {
            if (tick != Tick || (Id == fromClient && t == type)) return;

            int damage = -1;
            switch (desiredWeaponIndex)
            {
                case 1000:
                    damage = 150;
                    break;
                case 1004:
                    damage = 65;
                    break;
                case 1002:
                    damage = 120;
                    break;
                
            }
            
            if (ServerPlayer.list.TryGetValue(fromClient, out var fromPlayer))
            {
                var weapon = fromPlayer.GetCurrentWeapon();

                if (weapon != null)
                {
                    float factor = fromPlayer.HasPerk(Perk.Nerd) ? 1.3f : 1f;

                    if (damage == -1)
                    {
                        if (fromPlayer.IsInfected)
                        {
                            damage = 100;
                        }
                        else
                        {
                            damage = (int)(weapon.Stat.damage * factor);
                        }
                        
                    }

                    damage += (int) (factor * Random.Range(-weapon.Stat.damageOffset,
                        weapon.Stat.damageOffset));
                                
                    if (headShot)
                        damage *= 2;
                    if (wall)
                        damage /= 3;

                    switch (type)
                    {
                        case HitboxType.Player:
                            if (ServerPlayer.list.TryGetValue(Id, out var player))
                            {
                                player.TakeDamage(damage,fromClient,tick,headShot,wall, desiredWeaponIndex==-1 ? (ushort)fromPlayer.Weapons[fromPlayer.CurrentWeaponIndex] : (ushort)desiredWeaponIndex, isAiming);
                            }
                            break;
                        default:
                            if (ServerEnemy.list.TryGetValue(Id, out var enemy))
                                enemy.TakeDamage(damage, fromClient,tick,headShot,wall,desiredWeaponIndex==-1 ? (ushort)fromPlayer.Weapons[fromPlayer.CurrentWeaponIndex] : (ushort)desiredWeaponIndex);
                            break;
                    }
                }
            }

            // switch (type)
            // {
            //     case HitboxType.Player:
            //         if (ServerPlayer.list.TryGetValue(Id, out var player))
            //         {
            //             if (ServerPlayer.list.TryGetValue(fromClient, out var fromPlayer))
            //             {
            //                 var weapon = fromPlayer.GetCurrentWeapon();
            //
            //                 if (weapon != null)
            //                 {
            //                     float factor = fromPlayer.HasPerk(Perk.Nerd) ? 1.3f : 1f;
            //
            //                     if(damage==-1) damage = (int)(weapon.Stat.damage * factor);
            //
            //                     damage += (int) (factor * Random.Range(-weapon.Stat.damageOffset,
            //                         weapon.Stat.damageOffset));
            //
            //                     if (headShot)
            //                         damage *= 2;
            //                     if (wall)
            //                         damage /= 2;
            //                     
            //                     player.TakeDamage(damage,fromClient,headShot,wall, desiredWeaponIndex==-1 ? (ushort)fromPlayer.Weapons[fromPlayer.CurrentWeaponIndex] : (ushort)desiredWeaponIndex,tick, ServerPlayer.DamageType.Player);
            //                 }
            //             }
            //         }
            //         break;
            //     
            //     default:
            //         if (ServerEnemy.list.TryGetValue(Id, out var enemy))
            //         {
            //             if (ServerPlayer.list.TryGetValue(fromClient, out var fromPlayer))
            //             {
            //                 var weapon = fromPlayer.GetCurrentWeapon();
            //
            //                 if (weapon != null)
            //                 {
            //                     float factor = fromPlayer.HasPerk(Perk.Nerd) ? 1.5f : 1f;
            //
            //                     if(damage==-1)damage = (int)(weapon.Stat.damage * factor);
            //
            //                     damage += (int) (factor * Random.Range(-weapon.Stat.damageOffset,
            //                         weapon.Stat.damageOffset));
            //
            //                     if (wall)
            //                         damage /= 2;
            //                     
            //                     enemy.TakeDamage(damage, fromClient,tick);
            //                 }
            //             }
            //         }
            //         
            //         break;
            // }
        }
    }
}