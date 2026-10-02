using System;
using System.Collections;
using Manager;
using Multiplayer;
using Multiplayer.Entity.Server;
using Riptide;
using UnityEngine;
using Weapon.WeaponStats;

namespace Weapon
{
    public class ActiveWeapon
    {
        public WeaponStat Stat { get; private set; }

        private ServerPlayer _player;

        public ushort Index;
        
        public uint CurrentAmmo = 0;

        public bool IsReloading=false;

        private uint _lastFiredTick;

        private ushort _tickFireRate = 0;

        private Coroutine _reloadCoroutine;

        /// <summary>Tick at which the in-flight reload finishes.</summary>
        private uint _reloadEndTick;

        /// <summary>
        /// Tick at which ammo must be restored for a reload that was interrupted by
        /// a weapon switch. Non-zero means a refill is still owed.
        /// </summary>
        private uint _reloadRefreshTick;

        public ActiveWeapon(ServerPlayer player, ushort index)
        {
            _player = player;
            Index = index;
        }

        public void ResetTick()
        {
            _lastFiredTick = 0;
        }


        public void Init(WeaponStat stat)
        {
            Stat = stat;

            _tickFireRate = (ushort)(1 / stat.fireRate / Time.fixedDeltaTime);

            StopReloadCoroutine();

            CurrentAmmo = stat.maxAmmo;
            
            ResetTick();
        }

        public void Enable()
        {
            // A reload that was cut short by putting the weapon away still owes the
            // clip its ammo once its timer has run out.
            CompletePendingReload();
        }

        public void Disable()
        {
            // A pending reload must not be dropped: its coroutine is the only thing
            // that refills the clip, so interrupting it outright would leave the
            // weapon permanently empty. Kept in flight, it still has to run down its
            // full timer, so putting the weapon away can never shorten a reload.
            _reloadRefreshTick = _reloadEndTick;
            IsReloading = false;

            StopReloadCoroutine();
        }

        void StopReloadCoroutine()
        {
            if (_reloadCoroutine != null)
            {
                _player.StopCoroutine(_reloadCoroutine);
                _reloadCoroutine = null;
            }

            IsReloading = false;
        }

        void CompletePendingReload()
        {
            if (_reloadRefreshTick == 0) return;

            if (Stat != null &&
                NetworkServerManager.Instance.CurrentTick >= _reloadRefreshTick)
            {
                CurrentAmmo = Stat.maxAmmo;
            }

            _reloadRefreshTick = 0;
        }

        public EShootingResult DoAttack()
        {
            if (!IsAllowAttacking())
            {
                return EShootingResult.EResultTooFast;
            }
            if (CurrentAmmo <= 0)
            {
                return EShootingResult.EResultNoAmmo;
            }
            if (IsReloading)
            {
                return EShootingResult.EResultReloading;
            }
            if (_player.Dead)
            {
                return EShootingResult.EResultPlayerDead;
            }
            if (NetworkServerManager.GameState != GameState.MidMatch && (int)NetworkServerManager.ServerGameMode > 2)
            {
                return EShootingResult.EResultNotAllowed;
            }

            _lastFiredTick = NetworkServerManager.Instance.CurrentTick;

            switch (Stat.weaponType)
            {
                case WeaponStat.WeaponType.Knife:
                    break;
                case WeaponStat.WeaponType.Boomer:
                    CurrentAmmo--;
                    break;
                case WeaponStat.WeaponType.Taser:
                    break;
                default:
                    CurrentAmmo--;
                    break;
            }

            return EShootingResult.EResultOk;
        }

        public void DoReload(uint tick,ushort defaultAmmo = 0)
        {
            if (Stat.cantReload && NetworkServerManager.ServerGameMode != GameMode.GunGame) return;

            // Already full: never zero the clip. This also covers a redundant
            // request from a client that is out of sync with the server.
            if (CurrentAmmo >= Stat.maxAmmo) return;

            // Never trust the tick that came over the wire for the reload length.
            // The client tick is only a weak reference; if it lags far enough
            // behind the server tick the uint arithmetic below underflows and the
            // coroutine waits for years, leaving the weapon stuck at zero ammo.
            uint requestTick = Math.Max(tick, NetworkServerManager.Instance.CurrentTick);

            StopReloadCoroutine();

            uint offset = Stat.maxAmmo - CurrentAmmo;
            if (offset < 1) offset = 1;

            CurrentAmmo = defaultAmmo;
            IsReloading = true;
            _reloadRefreshTick = 0;

            _reloadCoroutine=_player.StartCoroutine(SetReload(requestTick,offset));
        }

        /// <summary>
        /// Matches Firearms.reloadMultiplier on the client (QuickHand) so both
        /// sides agree on how long a reload lasts.
        /// </summary>
        float ReloadMultiplier()
        {
            return _player.HasPerk(Perk.QuickHand) ? 0.6f : 1f;
        }

        IEnumerator SetReload(uint requestTick,uint offset)
        {
            // The reload interval has to be identical to the one the client runs
            // locally (Firearms.Reload times reloadMultiplier times the shell/spin
            // count), otherwise the client restores its clip on a different tick
            // than the server does.
            uint reloadTicks = (uint)(Stat.reloadTime * ReloadMultiplier() / Time.fixedDeltaTime);

            // Per-shell weapons (shotgun family) spend one interval per missing
            // round; every other weapon reloads in a single interval.
            uint waitTicks = Stat.weaponType == WeaponStat.WeaponType.ShotGun
                ? reloadTicks * offset
                : reloadTicks;

            // Anchor on the server's own clock so the wait can never go negative.
            uint startTick = Math.Max(requestTick, NetworkServerManager.Instance.CurrentTick);
            uint endTick = startTick + waitTicks;

            _reloadEndTick = endTick;

            while (NetworkServerManager.Instance.CurrentTick < endTick)
            {
                yield return new WaitForFixedUpdate();
            }

            _reloadCoroutine = null;
            _reloadEndTick = 0;
            _reloadRefreshTick = 0;
            IsReloading = false;
            CurrentAmmo = Stat.maxAmmo;
            
            Message message = Message.Create(MessageSendMode.Reliable,(ushort) ServerToClientId.WeaponReloaded);

            message.Add(_player.Id);
            message.Add(requestTick);
            message.Add(Index);
            
            NetworkServerManager.Instance.Server.SendToAll(message);
        }
        bool IsAllowAttacking()
        {
            // Debug.Log($"{tick - _tickFireRate} -- {_lastFiredTick}");
            return NetworkServerManager.Instance.CurrentTick - _tickFireRate + 1 >= _lastFiredTick;
        }
    }
}