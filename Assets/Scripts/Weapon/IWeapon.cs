

namespace Weapon
{
    public interface IWeapon
    {
        public void DoAttack();
        public void DoAim();
        public void DoNoAim();
        public void DoReload(int defaultAmmo, bool selfControl);
        public void Select(bool doAnimation);
        public void DeSelect();
    }
}
