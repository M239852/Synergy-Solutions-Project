using System;

/// <summary>
/// JANUS — Static Event Bus
///
/// Central pub/sub hub. Any system fires events here;
/// any other system subscribes without needing a direct reference.
///
/// Usage:
///   Fire:      JANUSEvents.OnMenuOpened?.Invoke();
///   Subscribe: JANUSEvents.OnMenuOpened += MyMethod;
///   Unsub:     JANUSEvents.OnMenuOpened -= MyMethod;
/// </summary>
public static class JANUSEvents
{
    // ── Menu Visibility ──────────────────────────────────────────────────
    /// <summary>Fired when the JANUS menu becomes visible.</summary>
    public static Action OnMenuOpened;

    /// <summary>Fired when the JANUS menu is hidden.</summary>
    public static Action OnMenuClosed;

    // ── Floor Plan ───────────────────────────────────────────────────────
    /// <summary>Fired when the patient/clinician selects an environment layout.</summary>
    public static Action<FloorPlanData> OnFloorPlanSelected;

    // ── Module / Session ─────────────────────────────────────────────────
    /// <summary>Fired when a cognitive assessment module is selected.</summary>
    public static Action<string> OnModuleSelected;

    /// <summary>Fired when a module begins (module name passed).</summary>
    public static Action<string> OnModuleBegin;

    /// <summary>Fired when the clinician pauses the session.</summary>
    public static Action OnSessionPaused;

    /// <summary>Fired when the session resumes after a pause.</summary>
    public static Action OnSessionResumed;

    /// <summary>Fired when the session ends (patientID, durationSeconds).</summary>
    public static Action<string, float> OnSessionEnded;

    // ── Hardware ─────────────────────────────────────────────────────────
    /// <summary>Fired when hardware monitoring detects a critical state (e.g. low battery).</summary>
    public static Action<string> OnHardwareWarning;

    // ── Gaze Dwell ───────────────────────────────────────────────────────
    /// <summary>Fired each frame while gaze dwell is in progress. Value: 0–1 progress.</summary>
    public static Action<float> OnGazeDwellProgress;
}
