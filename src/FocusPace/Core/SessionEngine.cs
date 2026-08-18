using FocusPace.Models;

namespace FocusPace.Core;

public sealed class GoalReachedEventArgs(SessionPhase phase, TimeSpan target) : EventArgs
{
    public SessionPhase Phase { get; } = phase;
    public TimeSpan Target { get; } = target;
}

public sealed class SessionEngine
{
    private static readonly TimeSpan BootMarkerTolerance = TimeSpan.FromMinutes(3);
    private readonly IClock _clock;
    private TimeSpan _accumulated;
    private DateTimeOffset? _runningSinceUtc;

    public SessionEngine(IClock clock)
    {
        _clock = clock;
    }

    public event EventHandler? StateChanged;
    public event EventHandler<GoalReachedEventArgs>? GoalReached;

    public SessionPhase Phase { get; private set; } = SessionPhase.Ready;
    public bool IsPaused { get; private set; }
    public TimeSpan Target { get; private set; }
    public bool GoalAnnounced { get; private set; }
    public bool IsGoalReached => Phase != SessionPhase.Ready && Target > TimeSpan.Zero && Elapsed >= Target;

    public TimeSpan Elapsed
    {
        get
        {
            var elapsed = _accumulated;
            if (_runningSinceUtc is { } started)
            {
                var currentRun = _clock.UtcNow - started;
                if (currentRun > TimeSpan.Zero)
                {
                    elapsed += currentRun;
                }
            }

            return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }
    }

    public void Start(SessionPhase phase, TimeSpan target)
    {
        if (phase is SessionPhase.Ready)
        {
            throw new ArgumentOutOfRangeException(nameof(phase));
        }

        if (target <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        Phase = phase;
        Target = target;
        _accumulated = TimeSpan.Zero;
        _runningSinceUtc = _clock.UtcNow;
        IsPaused = false;
        GoalAnnounced = false;
        OnStateChanged();
    }

    public void Pause()
    {
        if (Phase == SessionPhase.Ready || IsPaused)
        {
            return;
        }

        _accumulated = Elapsed;
        _runningSinceUtc = null;
        IsPaused = true;
        OnStateChanged();
    }

    public void Resume()
    {
        if (Phase == SessionPhase.Ready || !IsPaused)
        {
            return;
        }

        _runningSinceUtc = _clock.UtcNow;
        IsPaused = false;
        OnStateChanged();
    }

    public void Restart()
    {
        if (Phase == SessionPhase.Ready)
        {
            return;
        }

        _accumulated = TimeSpan.Zero;
        _runningSinceUtc = _clock.UtcNow;
        IsPaused = false;
        GoalAnnounced = false;
        OnStateChanged();
    }

    public void UpdateTarget(TimeSpan target)
    {
        if (Phase == SessionPhase.Ready)
        {
            return;
        }

        if (target <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(target));
        }

        if (Target == target)
        {
            return;
        }

        Target = target;
        if (!IsGoalReached)
        {
            GoalAnnounced = false;
        }

        OnStateChanged();
    }

    public void ResetToReady()
    {
        Phase = SessionPhase.Ready;
        Target = TimeSpan.Zero;
        _accumulated = TimeSpan.Zero;
        _runningSinceUtc = null;
        IsPaused = false;
        GoalAnnounced = false;
        OnStateChanged();
    }

    public void Pulse()
    {
        if (!GoalAnnounced && IsGoalReached)
        {
            GoalAnnounced = true;
            OnStateChanged();
            GoalReached?.Invoke(this, new GoalReachedEventArgs(Phase, Target));
        }
    }

    public SessionSnapshot CreateSnapshot() => new()
    {
        Phase = Phase,
        IsPaused = IsPaused,
        TargetTicks = Target.Ticks,
        AccumulatedTicks = _accumulated.Ticks,
        RunningSinceUtc = _runningSinceUtc,
        GoalAnnounced = GoalAnnounced,
        BootMarkerUtc = _clock.BootMarkerUtc
    };

    public bool TryRestore(SessionSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Phase == SessionPhase.Ready || snapshot.TargetTicks <= 0)
        {
            return false;
        }

        if ((_clock.BootMarkerUtc - snapshot.BootMarkerUtc).Duration() > BootMarkerTolerance)
        {
            return false;
        }

        Phase = snapshot.Phase;
        IsPaused = snapshot.IsPaused;
        Target = TimeSpan.FromTicks(snapshot.TargetTicks);
        _accumulated = TimeSpan.FromTicks(Math.Max(0, snapshot.AccumulatedTicks));
        _runningSinceUtc = snapshot.IsPaused ? null : snapshot.RunningSinceUtc ?? _clock.UtcNow;
        GoalAnnounced = snapshot.GoalAnnounced;
        OnStateChanged();
        return true;
    }

    private void OnStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
