namespace CamControl;

// UI-thread confined. The transport owns serialization; this class owns input,
// cancellation and lifecycle ordering. Neither class depends on WPF controls.
internal sealed class CameraController(CamControlService service) : IDisposable
{
    private CancellationTokenSource lifetime = new(), direction = new(), zoom = new();
    private CancellationTokenSource? position;
    private Task<bool>? stopping;
    private bool active = true, suspended, busy, closing, disposed, directionHeld, zoomHeld;
    private int errorVersion;
    public string Error { get; private set; } = "";
    public bool CanControl => active && !suspended && !closing && !disposed && stopping == null;
    public bool ControlsEnabled => CanControl && !busy;
    public bool IsMoving => directionHeld || zoomHeld;
    public event Action? Changed;
    private void Notify() => Changed?.Invoke();
    private void Report(string action, string error)
    {
        if (disposed) return;
        errorVersion++;
        Error = $"{action}: {error}";
        Notify();
    }
    internal void ReportStopFailure(string error) => Report(Texts.Error_CloseStop, error);

    private async Task<bool> ExecuteAsync(Func<CancellationToken, Task> command, string action,
        CancellationToken cancellation = default)
    {
        if (!CanControl) return false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellation);
        int version = errorVersion;
        bool success = await CameraCommandRunner.RunAsync(() => command(linked.Token), e => Report(action, e), linked.Token);
        if (success && version == errorVersion && CanControl) { Error = ""; Notify(); }
        return success;
    }

    public Task StartDirectionAsync(Direction value)
    {
        if (!ControlsEnabled || directionHeld) return Task.CompletedTask;
        directionHeld = true;
        return ExecuteAsync(t => service.MoveInDirection(value, t), Texts.Error_Move, direction.Token);
    }
    public Task StartZoomAsync(Zoom value)
    {
        if (!ControlsEnabled || zoomHeld) return Task.CompletedTask;
        zoomHeld = true;
        return ExecuteAsync(t => service.Zoom(value, t), Texts.Error_Zoom, zoom.Token);
    }
    public Task StopDirectionAsync()
    {
        if (!directionHeld) return Task.CompletedTask;
        directionHeld = false;
        direction.Cancel(); direction.Dispose(); direction = new();
        return ExecuteAsync(service.StopDirectionalMovement, Texts.Error_MovementStop);
    }
    public Task StopZoomAsync()
    {
        if (!zoomHeld) return Task.CompletedTask;
        zoomHeld = false;
        zoom.Cancel(); zoom.Dispose(); zoom = new();
        return ExecuteAsync(service.StopZooming, Texts.Error_ZoomStop);
    }

    public Task<bool> StopAsync()
    {
        position?.Cancel();
        return StopManualAsync();
    }
    private Task<bool> StopManualAsync()
    {
        directionHeld = zoomHeld = false;
        if (disposed) return Task.FromResult(false);
        if (stopping != null) return stopping;
        var previous = lifetime;
        previous.Cancel(); lifetime = new();
        // Publish the shared stop before invoking the transport, including when
        // a fake/demo transport completes synchronously or Changed re-enters.
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        stopping = completion.Task;
        Notify();
        _ = CompleteStopAsync(previous, completion);
        return completion.Task;
    }
    private async Task CompleteStopAsync(CancellationTokenSource previous, TaskCompletionSource<bool> completion)
    {
        try
        {
            bool result = await CameraCommandRunner.RunAsync(service.StopAll, e => Report(Texts.Error_AllStops, e));
            stopping = null;
            previous.Dispose();
            Notify();
            completion.TrySetResult(result);
        }
        catch (Exception e) { stopping = null; previous.Dispose(); Notify(); completion.TrySetException(e); }
    }

    public Task<bool> RecallAsync(int number) => PositionAsync(number, false);
    public Task<bool> SaveAsync(int number) => PositionAsync(number, true);
    private async Task<bool> PositionAsync(int number, bool save)
    {
        if (!ControlsEnabled) return false;
        using var cancellation = new CancellationTokenSource();
        position = cancellation; busy = true; Notify();
        try
        {
            if (!await StopManualAsync() || cancellation.IsCancellationRequested) return false;
            return await ExecuteAsync(t => save ? service.SavePreset(number, t) : service.MoveToPreset(number, t),
                save ? Texts.Preset_Save : Texts.Preset_Recall, cancellation.Token);
        }
        finally { position = null; busy = false; Notify(); }
    }
    public async Task SetActiveAsync(bool value)
    {
        active = value; Notify();
        if (!value && !disposed) await StopAsync();
    }
    public async Task SuspendAsync()
    {
        suspended = true; Notify();
        await StopAsync();
    }
    public void Resume() { suspended = false; Notify(); }
    public async Task<bool> CloseAsync(int? exitPreset = null)
    {
        if (closing || disposed) return false;
        closing = true; Notify();
        bool stopped = await StopAsync();
        if (stopped && exitPreset.HasValue)
            return await CameraCommandRunner.RunAsync(() => service.MoveToPreset(exitPreset.Value), e => Report(Texts.Error_ExitPosition, e));
        return stopped;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; position?.Cancel();
        lifetime.Cancel(); direction.Cancel(); zoom.Cancel();
        lifetime.Dispose(); direction.Dispose(); zoom.Dispose();
    }
}
