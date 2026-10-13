using System;
using System.Collections.Generic;
using System.Linq;

namespace Multiplayer.Migration
{
    public static class SuccessorElection
    {
        public const int UnknownPing = 1000;

        public static List<ulong> Rank(IReadOnlyCollection<ulong> candidates, Func<ulong, ulong, int> ping)
        {
            var unique = candidates.Distinct().ToList();
            var scores = new Dictionary<ulong, float>();
            foreach (var c in unique)
            {
                float total = 0;
                int n = 0;
                foreach (var o in unique)
                {
                    if (o == c) continue;
                    int p = ping(c, o);
                    total += p < 0 ? UnknownPing : p;
                    n++;
                }
                scores[c] = n == 0 ? 0 : total / n;
            }

            return unique.OrderBy(c => scores[c]).ThenBy(c => c).ToList();
        }
    }
}
