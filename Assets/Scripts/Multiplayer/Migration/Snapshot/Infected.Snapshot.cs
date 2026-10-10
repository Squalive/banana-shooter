using Multiplayer.Migration;

namespace Mode
{
    public partial class Infected
    {
        internal override string DescribeMode() => base.DescribeMode() + $" infected{(infected ? 1 : 0)} infecttime{_infectTime} next{this.Remaining(nameof(FirstInfect)):F1}";

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
