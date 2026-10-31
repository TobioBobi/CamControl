using System.Diagnostics;
using System.Windows.Threading;

namespace CamControl;

internal sealed class CameraSession : IDisposable
{
    public static readonly int[] Buttons = [1, 2, 3, 4, 5, 6, 9];
    private readonly Dictionary<int, string> names = PresetSettings.Load(), demoNames = PresetSettings.Defaults();
    private readonly Dictionary<int, int> numbers = PresetSettings.LoadNumbers(), demoNumbers = PresetSettings.DefaultNumbers();
    private AutomaticPositions automatic = AutomaticPositions.Load(), demoAutomatic = new();
    private HttpClient? demoClient;
    private DispatcherTimer? demoTimer;
    private readonly Stopwatch demoClock = new();
    public DemoCamera Demo { get; } = new();
    public bool IsDemo { get; private set; }
    public CamControlService Service { get; private set; }
    public CameraController Controller { get; private set; }
    public IReadOnlyDictionary<int, string> Names => IsDemo ? demoNames : names;
    public IReadOnlyDictionary<int, int> Numbers => IsDemo ? demoNumbers : numbers;
    public AutomaticPositions Automatic => IsDemo ? demoAutomatic : automatic;
    public CameraSession(bool demo)
    {
        Service = new(); Controller = new(Service);
        if (demo) SwitchMode(true);
    }
    // An injected transport allows full-window command/lifecycle checks without
    // contacting hardware or inheriting the user's automatic startup choices.
    internal CameraSession(CamControlService service)
    {
        Service = service; Controller = new(Service); automatic = new();
    }
    // Caller has suspended input and awaited all stops on the previous service.
    public void SwitchMode(bool demo)
    {
        if (demo == IsDemo) return;
        StopDemoSimulation();
        string ip = Service.CameraIp;
        Controller.Dispose(); Service.Dispose(); demoClient?.Dispose(); demoClient = null;
        if (demo)
        {
            Demo.Apply($"?ptzcmd&poscall&{(demoAutomatic.StartPreset is { } start ? demoNumbers[start] : 4)}");
            demoClient = new(new DemoCameraHandler(Demo));
            Service = new(demoClient, ip);
            // One simulation clock per session, independent of preview visibility
            // and of how many previews (main window/editor) are loaded.
            demoTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
            demoTimer.Tick += (_, _) => { Demo.Advance(demoClock.Elapsed); demoClock.Restart(); };
            demoClock.Restart(); demoTimer.Start();
        }
        else { Service = new(); Service.SetCameraIp(ip); }
        IsDemo = demo; Controller = new(Service);
    }
    public void SaveMapping(int button, string name, int number)
    {
        if (!PresetSettings.IsValidName(name) || !PresetSettings.IsValidNumber(number) || Numbers.Any(p => p.Key != button && p.Value == number))
            throw new ArgumentException(Texts.Preset_InvalidMapping);
        var updatedNames = new Dictionary<int, string>(Names) { [button] = name.Trim() };
        var updatedNumbers = new Dictionary<int, int>(Numbers) { [button] = number };
        if (!IsDemo) PresetSettings.SaveConfiguration(updatedNames, updatedNumbers);
        (IsDemo ? demoNames : names)[button] = name.Trim();
        (IsDemo ? demoNumbers : numbers)[button] = number;
    }
    public void SaveAutomatic(AutomaticPositions value)
    {
        if (!AutomaticPositions.IsValid(value.StartPreset) || !AutomaticPositions.IsValid(value.ExitPreset)) throw new ArgumentException(Texts.Preset_InvalidPosition);
        if (!IsDemo) AutomaticPositions.Save(value);
        if (IsDemo) demoAutomatic = value; else automatic = value;
    }
    private void StopDemoSimulation() { demoTimer?.Stop(); demoTimer = null; demoClock.Reset(); }
    public void Dispose() { StopDemoSimulation(); Controller.Dispose(); Service.Dispose(); demoClient?.Dispose(); }
}
