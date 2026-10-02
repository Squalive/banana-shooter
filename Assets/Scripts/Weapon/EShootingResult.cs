namespace Weapon
{
    public enum EShootingResult : int
    {
        EResultOk = 0,
        EResultTooFast,
        EResultNoAmmo,
        EResultReloading,
        EResultPlayerDead,
        EResultNotAllowed
    }
}