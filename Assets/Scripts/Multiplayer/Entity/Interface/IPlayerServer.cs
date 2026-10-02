using Multiplayer.Entity.Server;
using Weapon;

namespace Multiplayer.Entity.Interface
{
    public interface IPlayerServer : IPlayer
    {
        void SendPlayerMovement();
        void DisableInvincible();
        ActiveWeapon GetCurrentWeapon();
        void StartSpecialWeaponTimer();
        void Respawn();
        void StopSpecialWeaponTimer();
        void Spect(bool flag);
        void SetPerks();
        void SendSpawn(ushort toClient);
        void PlayerGetWeapon(short[] weapons);
        void SetHasBanana(bool flag);
        void SetCrazy(string str);
        void AddCash(int c);
        void SetWeapon();
        void Kick(string reason="");
        void Ban();
        void Kill(ushort attacker = 3000, bool headShot=false, bool wallbang=false,bool isAiming=false,ushort weapon = 1000,ServerPlayer.DamageType damageType = ServerPlayer.DamageType.Void);
        
    }
}