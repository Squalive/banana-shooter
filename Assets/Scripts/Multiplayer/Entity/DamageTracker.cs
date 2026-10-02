using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer.Entity
{
    public class DamageTracker
    {
        private Dictionary<ushort, int> _damageTaken = new(), _damageGiven = new ();

        public void DamageTaken(ushort from, int damage)
        {
            if (_damageTaken.ContainsKey(from))
            {
                _damageTaken[from] += damage;
            }
            else
            {
                _damageTaken[from] = damage;
            }
        }

        public void DamageGiven(ushort to, int damage)
        {
            if (_damageGiven.ContainsKey(to))
            {
                _damageGiven[to] += damage;
            }
            else
            {
                _damageGiven[to] = damage;
            }
        }

        public void Clear()
        {
            _damageTaken.Clear();
            _damageGiven.Clear();
        }

        public int GetDamageTaken(ushort from)
        {
            if (_damageTaken.TryGetValue(from, out var value))
            {
                return value;
            }

            return 0;
        }

        public int GetDamageGiven(ushort to)
        {
            if (_damageGiven.TryGetValue(to, out var value))
            {
                return value;
            }

            return 0;
        }
    }
}