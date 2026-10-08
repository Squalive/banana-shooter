using Multiplayer.Migration;

namespace Mode
{
    public partial class KingOfTheHill
    {
        internal override void ClearServerSide()
        {
            base.ClearServerSide();
            this.Unschedule(nameof(HillCountdown));
            this.Unschedule(nameof(SetNewHillServer));
        }

        protected override void WriteModeSnapshot(SnapshotWriter w)
        {
            w.Int(serverIndex);
            w.UShort(_hillTime);
            w.WriteInvoke(this, nameof(HillCountdown));
            w.WriteInvoke(this, nameof(SetNewHillServer));
        }

        protected override void ReadModeSnapshot(SnapshotReader r)
        {
            serverIndex = r.Int();
            _hillTime = r.UShort();
            r.ReadInvoke(this, nameof(HillCountdown));
            r.ReadInvoke(this, nameof(SetNewHillServer));
        }
    }
}
