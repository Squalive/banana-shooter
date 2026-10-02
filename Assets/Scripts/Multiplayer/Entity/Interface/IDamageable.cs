using Multiplayer.Entity.Server;

namespace Multiplayer.Entity.Interface
{
    public interface IDamageable
    {
        int MaxHealth { get; set; }
        
        int Health { get; set; }

        bool Dead { get; set; }

        void TakeDamage(int damage,ushort attacker,uint tick,bool headShot,bool wallbang,ushort weapon, params object[] param);

        bool CanDamage(ushort attacker,ServerPlayer.DamageType damageType);
    }
}