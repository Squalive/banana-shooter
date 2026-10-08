using Multiplayer.Migration;

namespace Mode
{
    public partial class Infected
    {
        internal override void ClearServerSide()
        {
            base.ClearServerSide();
            this.Unschedule(nameof(FirstInfect));
        }

        protected override void WriteModeSnapshot(SnapshotWriter w)
        {
            w.Bool(infected);
            w.UShort(_infectTime);
            w.WriteInvoke(this, nameof(FirstInfect));
        }

        protected override void ReadModeSnapshot(SnapshotReader r)
        {
            infected = r.Bool();
            _infectTime = r.UShort();
            r.ReadInvoke(this, nameof(FirstInfect));
        }
    }
}
