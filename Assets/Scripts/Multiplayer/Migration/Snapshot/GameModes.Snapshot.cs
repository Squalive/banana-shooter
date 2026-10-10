using Multiplayer.Migration;

public abstract partial class GameModes
{
    internal void WriteSnapshot(SnapshotWriter w)
    {
        w.Bool(started);
        w.Bool(stopped);
        w.Bool(_init);
        w.UInt(StartTick);
        w.UInt(EndTick);
        w.Float(leftTime);
        w.Ints(gunKills);
        w.Bool(complete);
        WriteModeSnapshot(w);
    }

    internal void ReadSnapshot(SnapshotReader r)
    {
        started = r.Bool();
        stopped = r.Bool();
        _init = r.Bool();
        StartTick = r.UInt();
        EndTick = r.UInt();
        leftTime = r.Float();
        gunKills = r.Ints() ?? gunKills;
        complete = r.Bool();
        ReadModeSnapshot(r);
    }

    internal virtual string DescribeMode() => $" started{(started ? 1 : 0)} stopped{(stopped ? 1 : 0)}";

    internal virtual void ClearServerSide()
    {
        _init = false;
    }

    protected virtual void WriteModeSnapshot(SnapshotWriter w)
    {
    }

    protected virtual void ReadModeSnapshot(SnapshotReader r)
    {
    }
}
