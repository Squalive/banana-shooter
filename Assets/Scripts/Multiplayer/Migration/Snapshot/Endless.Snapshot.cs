using Manager;
using Multiplayer.Migration;

namespace Mode
{
    public partial class Endless
    {
        internal string DescribeState() => $" endless{(serverStarted ? 1 : 0)} round{_round} spawn{_spawnCount} wave{_waveTime:F1} diff{difficulty:F2} t{serverTime:F1}";

        internal void ClearServerSide()
        {
            serverStarted = false;
            this.Unschedule(nameof(Restart));
            GameManager.Instance.ServerPlayerDead -= ServerPlayerDead;
        }

        internal void WriteSnapshot(SnapshotWriter w)
        {
            w.Bool(serverStarted);
            w.Float(serverTime);
            w.Float(difficulty);
            w.UShort(_round);
            w.UShort(_spawnCount);
            w.Float(_waveTime);
            w.Float(_waveFresh);
            w.Float(_spawnTime);
            w.Float(serverStartTime);
            w.WriteInvoke(this, nameof(Restart));
        }

        internal void ReadSnapshot(SnapshotReader r)
        {
            serverStarted = r.Bool();
            serverTime = r.Float();
            difficulty = r.Float();
            _round = r.UShort();
            _spawnCount = r.UShort();
            _waveTime = r.Float();
            _waveFresh = r.Float();
            _spawnTime = r.Float();
            serverStartTime = r.Float();
            r.ReadInvoke(this, nameof(Restart));

            GameManager.Instance.ServerPlayerDead -= ServerPlayerDead;
            GameManager.Instance.ServerPlayerDead += ServerPlayerDead;
        }
    }
}
