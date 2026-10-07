using Multiplayer.Migration;

namespace Weapon
{
    public partial class ActiveWeapon
    {
        private uint _reloadRequestTick;

        internal void WriteSnapshot(SnapshotWriter w)
        {
            w.UInt(CurrentAmmo);
            w.Bool(IsReloading);
            w.UInt(_lastFiredTick);
            w.UInt(_reloadRequestTick);
            w.UInt(_reloadEndTick);
            w.UInt(_reloadRefreshTick);
        }

        internal void ReadSnapshot(SnapshotReader r)
        {
            uint ammo = r.UInt();
            bool reloading = r.Bool();
            uint lastFired = r.UInt();
            uint requestTick = r.UInt();
            uint endTick = r.UInt();
            uint refreshTick = r.UInt();

            StopReloadCoroutine();
            CurrentAmmo = ammo;
            _lastFiredTick = lastFired;
            _reloadRefreshTick = refreshTick;
            _reloadRequestTick = requestTick;
            _reloadEndTick = endTick;

            if (reloading && Stat != null)
            {
                IsReloading = true;
                _reloadCoroutine = _player.StartCoroutine(WaitReload(requestTick, endTick));
            }
        }
    }
}
