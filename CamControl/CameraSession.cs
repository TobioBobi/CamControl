namespace CamControl;

internal sealed class CameraSession : IDisposable
{
    public static readonly int[] Buttons = [1, 2, 3, 4, 5, 6, 9];
    private readonly Dictionary<int, string> names = PresetSettings.Load(), demoNames = PresetSettings.Defaults();
    private readonly Dictionary<int, int> numbers = PresetSettings.LoadNumbers(), demoNumbers = PresetSettings.DefaultNumbers();
    private AutomaticPositions automatic = AutomaticPositions.Load(), demoAutomatic = new();
    private HttpClient? demoClient;
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
    // Caller has suspended input and awaited all stops on the previous service.
    public void SwitchMode(bool demo)
    {
        if (demo == IsDemo) return;
        string ip = Service.CameraIp;
        Controller.Dispose(); Service.Dispose(); demoClient?.Dispose(); demoClient = null;
        if (demo)
        {
            Demo.Apply($"?ptzcmd&poscall&{(demoAutomatic.StartPreset is { } start ? demoNumbers[start] : 4)}");
            demoClient = new(new DemoCameraHandler(Demo));
            Service = new(demoClient, ip);
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
    public void Dispose() { Controller.Dispose(); Service.Dispose(); demoClient?.Dispose(); }
}
