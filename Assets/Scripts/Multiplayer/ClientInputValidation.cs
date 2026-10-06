using System;

namespace Multiplayer
{
    public static class ClientInputValidation
    {
        public const int MaxInventoryBytes = 1 << 20;

        public static bool IsValidLoadout(short[] weapons, int slots, int weaponCount)
        {
            if (weapons == null || weapons.Length != slots) return false;

            foreach (var weapon in weapons)
            {
                if (weapon < -1 || weapon >= weaponCount) return false;
            }

            return true;
        }

        public static bool IsValidPerks(ushort[] perks, int slots, int maxPerk)
        {
            if (perks == null || perks.Length > slots) return false;

            foreach (var perk in perks)
            {
                if (perk > maxPerk) return false;
            }

            return true;
        }

        public static bool IsValidPurchase(int weaponId, int replaceIndex, int weaponCount, int slotCount)
        {
            return weaponId >= 0 && weaponId < weaponCount && replaceIndex >= 0 && replaceIndex < slotCount;
        }

        public static bool TryStartInventory(int length, byte[] firstChunk, out byte[] buffer, out int received)
        {
            buffer = null;
            received = 0;

            if (firstChunk == null || length < 0 || length > MaxInventoryBytes || firstChunk.Length > length) return false;

            buffer = new byte[length];
            Array.Copy(firstChunk, buffer, firstChunk.Length);
            received = firstChunk.Length;
            return true;
        }

        public static bool TryAppendInventory(byte[] buffer, ref int received, byte[] fragment)
        {
            if (buffer == null || fragment == null || fragment.Length > buffer.Length - received) return false;

            Array.Copy(fragment, 0, buffer, received, fragment.Length);
            received += fragment.Length;
            return true;
        }
    }
}
