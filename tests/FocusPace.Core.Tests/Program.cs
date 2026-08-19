using FocusPace.Core;
using FocusPace.Models;

var tests = new (string Name, Action Run)[]
{
    ("Focus accumulates forward", FocusAccumulatesForward),
    ("Pause excludes time away", PauseExcludesTimeAway),
    ("Goal fires once and overtime continues", GoalFiresOnceAndOvertimeContinues),
    ("Focus warns once with three minutes remaining", FocusWarnsOnceWithThreeMinutesRemaining),
    ("Changing target updates the active goal", ChangingTargetUpdatesActiveGoal),
    ("Same-boot session restores", SameBootSessionRestores),
    ("Different-boot session is rejected", DifferentBootSessionIsRejected)
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.WriteLine($"FAIL  {test.Name}: {exception.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed");
return failures;

static void FocusAccumulatesForward()
{
    var clock = new ManualClock();
    var engine = new SessionEngine(clock);
    engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(8));
    Equal(TimeSpan.FromMinutes(12) + TimeSpan.FromSeconds(8), engine.Elapsed);
}

static void PauseExcludesTimeAway()
{
    var clock = new ManualClock();
    var engine = new SessionEngine(clock);
    engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(10));
    engine.Pause();
    clock.Advance(TimeSpan.FromMinutes(30));
    Equal(TimeSpan.FromMinutes(10), engine.Elapsed);
    engine.Resume();
    clock.Advance(TimeSpan.FromMinutes(2));
    Equal(TimeSpan.FromMinutes(12), engine.Elapsed);
}

static void GoalFiresOnceAndOvertimeContinues()
{
    var clock = new ManualClock();
    var engine = new SessionEngine(clock);
    var goals = 0;
    engine.GoalReached += (_, _) => goals++;
    engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(45));
    engine.Pulse();
    engine.Pulse();
    Equal(1, goals);
    clock.Advance(TimeSpan.FromMinutes(4));
    Equal(TimeSpan.FromMinutes(49), engine.Elapsed);
}

static void FocusWarnsOnceWithThreeMinutesRemaining()
{
    var clock = new ManualClock();
    var engine = new SessionEngine(clock);
    var warnings = 0;
    engine.GoalApproaching += (_, e) =>
    {
        Equal(SessionPhase.Focus, e.Phase);
        True(e.Remaining <= TimeSpan.FromMinutes(3));
        warnings++;
    };
    engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(41) + TimeSpan.FromSeconds(59));
    engine.Pulse();
    Equal(0, warnings);
    clock.Advance(TimeSpan.FromSeconds(1));
    engine.Pulse();
    engine.Pulse();
    Equal(1, warnings);
}

static void ChangingTargetUpdatesActiveGoal()
{
    var clock = new ManualClock();
    var engine = new SessionEngine(clock);
    engine.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(10));
    engine.UpdateTarget(TimeSpan.FromMinutes(20));
    Equal(TimeSpan.FromMinutes(20), engine.Target);
    True(!engine.IsGoalReached);
    clock.Advance(TimeSpan.FromMinutes(10));
    True(engine.IsGoalReached);
}

static void SameBootSessionRestores()
{
    var clock = new ManualClock();
    var first = new SessionEngine(clock);
    first.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    clock.Advance(TimeSpan.FromMinutes(7));
    var snapshot = first.CreateSnapshot();
    clock.Advance(TimeSpan.FromSeconds(2));
    var restored = new SessionEngine(clock);
    True(restored.TryRestore(snapshot));
    Equal(TimeSpan.FromMinutes(7) + TimeSpan.FromSeconds(2), restored.Elapsed);
}

static void DifferentBootSessionIsRejected()
{
    var clock = new ManualClock();
    var first = new SessionEngine(clock);
    first.Start(SessionPhase.Focus, TimeSpan.FromMinutes(45));
    var snapshot = first.CreateSnapshot();
    clock.BootMarkerUtc -= TimeSpan.FromHours(1);
    var restored = new SessionEngine(clock);
    True(!restored.TryRestore(snapshot));
    Equal(SessionPhase.Ready, restored.Phase);
}

static void Equal<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void True(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

internal sealed class ManualClock : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);
    public DateTimeOffset BootMarkerUtc { get; set; } = new(2026, 8, 17, 22, 0, 0, TimeSpan.Zero);
    public void Advance(TimeSpan duration) => UtcNow += duration;
}
