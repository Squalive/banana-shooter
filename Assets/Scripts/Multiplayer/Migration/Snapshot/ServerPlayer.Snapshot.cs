using System.Collections.Generic;
using System.Linq;
using Manager;
using Multiplayer.Migration;
using UnityEngine;

namespace Multiplayer.Entity.Server
{
    public partial class ServerPlayer
    {
        private bool _restored;

        private static readonly string[] Timers =
        {
            nameof(Breathe), nameof(FireGone), nameof(ClearCrazy), nameof(Respawn),
            nameof(DisableInvincible), nameof(GetReadyToFire), nameof(ReadyToSendTime), nameof(ClearKick)
        };

        internal static void WriteAll(SnapshotWriter w)
        {
            var players = list.Values.OfType<ServerPlayer>().ToList();
            w.Int(players.Count);
            foreach (var p in players)
                p.WriteSnapshot(w);

            w.Int(RebelPlayers);
            w.Int(AlliancePlayers);

            w.Bool(Vote != null);
            if (Vote != null)
            {
                w.UShort(Vote.PlayerId);
                w.UShort(Vote.FromClient);
                w.UShort(Vote.agreeCount);
                w.UShort(Vote.disAgreeCount);
                w.Int(Vote.Players.Count);
                foreach (var pair in Vote.Players)
                {
                    w.UShort(pair.Key);
                    w.Bool(pair.Value);
                }
            }
        }

        internal static void ReadAll(SnapshotReader r)
        {
            int count = r.Int();
            for (int i = 0; i < count; i++)
                ReadPlayer(r);

            RebelPlayers = r.Int();
            AlliancePlayers = r.Int();

            Vote = null;
            if (r.Bool())
            {
                var vote = new VoteKicking(r.UShort(), r.UShort())
                {
                    agreeCount = r.UShort(),
                    disAgreeCount = r.UShort()
                };
                int voters = r.Int();
                for (int i = 0; i < voters; i++)
                    vote.Players[r.UShort()] = r.Bool();
                Vote = vote;
            }
        }

        void WriteSnapshot(SnapshotWriter w)
        {
            w.UShort(Id);
            w.String(Username);
            w.ULong(SteamId);
            w.String(Description);
            w.Int(exp);
            w.ULong(GroupId);
            w.Bool(DisplayTag);
            w.Cosmetics(CosmeticIndex);
            w.Int(Perks.Count);
            foreach (var perk in Perks)
                w.UShort((ushort)perk);

            w.Vector3(transform.position);
            w.Vector3(PlayerTransform.position);
            w.Quaternion(PlayerTransform.rotation);
            w.Bool(player.gameObject.activeSelf);
            w.Bool(transform.GetChild(0).gameObject.activeSelf);
            w.Vector3(Velocity);
            w.Float(XRotation);
            w.Bool(Grounded);
            w.Bool(Crouch);
            w.Bool(IsAiming);
            w.Bool(IsGrappling);
            w.Vector3(GrapplePoint);

            w.Int(Health);
            w.Int(MaxHealth);
            w.Bool(Dead);
            w.UShort(Kills);
            w.UShort(Deaths);
            w.Float(StayTime);
            w.Int(Cash);
            w.Short(Coin);
            w.Int((int)Team);
            w.Bool(IsInfected);
            w.Bool(IsCrazy);
            w.Int(WeaponLevel);
            w.Bool(HasBanana);
            w.UShort(CurrentLifeKill);
            w.Bool(Eliminated);
            w.Bool(specting);
            w.Bool(Specting);
            w.Bool(invincible);

            w.Int(CurrentWeaponIndex);
            w.Shorts(Weapons);
            foreach (var weapon in ActiveWeapons)
                weapon.WriteSnapshot(w);

            w.Int(TacticalThrowCount);
            w.Float(throwablePower);
            w.Bool(IsFire);
            w.Bool(ReadyToFire);
            w.UShort(LastAttackBy);
            w.Float(leftTime);
            w.Float(warnTimer);
            w.Bool(alreadyWarn);
            w.Bool(sendTime);
            w.Float(latencyTimer);

            foreach (var timer in Timers)
                w.WriteInvoke(this, timer);
        }

        static void ReadPlayer(SnapshotReader r)
        {
            ushort id = r.UShort();
            string username = r.String();
            ulong steamId = r.ULong();
            string description = r.String();
            int exp = r.Int();
            ulong groupId = r.ULong();
            bool displayTag = r.Bool();
            var cosmetics = r.Cosmetics();
            int perkCount = r.Int();
            var perks = new List<Perk>();
            for (int i = 0; i < perkCount; i++)
                perks.Add((Perk)r.UShort());

            Vector3 rootPos = r.Vector3();

            ServerPlayer p = Instantiate(NetworkServerManager.Instance.ServerPlayerPrefab, rootPos, Quaternion.identity)
                .GetComponent<ServerPlayer>();
            p._restored = true;
            p.PlayerTransform = p.player.transform;

            p.Id = id;
            p.Username = username;
            p.SteamId = steamId;
            p.Description = description;
            p.exp = exp;
            p.GroupId = groupId;
            p.DisplayTag = displayTag;
            p.CosmeticIndex = cosmetics ?? new InventoryManager.CosmeticIndex();
            p.Perks = perks;

            p.PlayerTransform.position = r.Vector3();
            p.PlayerTransform.rotation = r.Quaternion();
            p.player.gameObject.SetActive(r.Bool());
            p.transform.GetChild(0).gameObject.SetActive(r.Bool());
            p.Velocity = r.Vector3();
            p.XRotation = r.Float();
            p.Grounded = r.Bool();
            p.Crouch = r.Bool();
            p.IsAiming = r.Bool();
            p.IsGrappling = r.Bool();
            p.GrapplePoint = r.Vector3();

            p.Health = r.Int();
            p.MaxHealth = r.Int();
            p.Dead = r.Bool();
            p.Kills = r.UShort();
            p.Deaths = r.UShort();
            p.StayTime = r.Float();
            p.Cash = r.Int();
            p.Coin = r.Short();
            p.Team = (Team)r.Int();
            p.IsInfected = r.Bool();
            p.IsCrazy = r.Bool();
            p.WeaponLevel = r.Int();
            p.HasBanana = r.Bool();
            p.CurrentLifeKill = r.UShort();
            p.Eliminated = r.Bool();
            p.specting = r.Bool();
            p.Specting = r.Bool();
            p.invincible = r.Bool();

            p.CurrentWeaponIndex = r.Int();
            var weapons = r.Shorts();
            p.ActiveWeapons = new[] { new Weapon.ActiveWeapon(p, 0), new Weapon.ActiveWeapon(p, 1), new Weapon.ActiveWeapon(p, 2), new Weapon.ActiveWeapon(p, 3) };
            for (int i = 0; i < p.Weapons.Length; i++)
            {
                p.Weapons[i] = weapons != null && i < weapons.Length ? weapons[i] : (short)-1;
                if (p.Weapons[i] >= 0 && p.Weapons[i] < NetworkServerManager.Instance.weaponInfo.Count)
                    p.ActiveWeapons[i].Init(NetworkServerManager.Instance.weaponInfo[p.Weapons[i]]);
                else
                    p.ActiveWeapons[i].Disable();
            }
            foreach (var weapon in p.ActiveWeapons)
                weapon.ReadSnapshot(r);

            p.TacticalThrowCount = r.Int();
            p.throwablePower = r.Float();
            p.IsFire = r.Bool();
            p.ReadyToFire = r.Bool();
            p.LastAttackBy = r.UShort();
            p.leftTime = r.Float();
            p.warnTimer = r.Float();
            p.alreadyWarn = r.Bool();
            p.sendTime = r.Bool();
            p.latencyTimer = r.Float();

            foreach (var timer in Timers)
                r.ReadInvoke(p, timer);

            list[id] = p;
            NetworkServerManager.Entity.Entities[id] = p;
        }
    }
}
