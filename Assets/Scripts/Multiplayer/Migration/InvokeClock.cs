using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Multiplayer.Migration
{
    public static class InvokeClock
    {
        private static readonly ConditionalWeakTable<MonoBehaviour, Dictionary<string, float>> Deadlines = new();

        public static void Schedule(this MonoBehaviour behaviour, string method, float delay)
        {
            behaviour.Invoke(method, delay);
            Deadlines.GetOrCreateValue(behaviour)[method] = Time.time + delay;
        }

        public static void Unschedule(this MonoBehaviour behaviour, string method)
        {
            behaviour.CancelInvoke(method);
            if (Deadlines.TryGetValue(behaviour, out var deadlines))
                deadlines.Remove(method);
        }

        public static float Remaining(this MonoBehaviour behaviour, string method)
        {
            if (!behaviour.IsInvoking(method) || !Deadlines.TryGetValue(behaviour, out var deadlines) || !deadlines.TryGetValue(method, out var deadline))
                return -1f;
            return Mathf.Max(0f, deadline - Time.time);
        }

        public static void WriteInvoke(this SnapshotWriter writer, MonoBehaviour behaviour, string method)
        {
            writer.Float(behaviour.Remaining(method));
        }

        public static void ReadInvoke(this SnapshotReader reader, MonoBehaviour behaviour, string method)
        {
            float remaining = reader.Float();
            behaviour.Unschedule(method);
            if (remaining >= 0f)
                behaviour.Schedule(method, remaining);
        }
    }
}
