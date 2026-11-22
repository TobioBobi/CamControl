using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace CamControl;

internal sealed record DiscoveredCamera(string DeviceId, string Ip, string? Name);

internal static class OnvifDiscovery
{
    private static readonly XNamespace Soap = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace Addressing = "http://schemas.xmlsoap.org/ws/2004/08/addressing";
    private static readonly XNamespace Discovery = "http://schemas.xmlsoap.org/ws/2005/04/discovery";
    private static readonly IPEndPoint Multicast = new(IPAddress.Parse("239.255.255.250"), 3702);
    private const string OnvifScope = "onvif://www.onvif.org/";

    public static Task<IReadOnlyList<DiscoveredCamera>> FindAsync(CancellationToken cancellationToken)
        => Task.Run<IReadOnlyList<DiscoveredCamera>>(async () =>
        {
            // Windows can route the default multicast socket onto an unrelated
            // adapter. Send one probe per active local IPv4 address.
            try
            {
                var addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(adapter => adapter.OperationalStatus == OperationalStatus.Up && adapter.SupportsMulticast &&
                        adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                    .Select(address => address.Address)
                    .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
                    .Distinct();
                var replies = await Task.WhenAll(addresses.Select(address => ProbeAsync(address, Multicast, cancellationToken)));
                return Collect(replies.SelectMany(reply => reply));
            }
            catch (NetworkInformationException)
            {
                return [];
            }
        }, cancellationToken);

    internal static async Task<IReadOnlyList<DiscoveredCamera>> ProbeAsync(
        IPAddress localAddress, IPEndPoint destination, CancellationToken cancellationToken)
    {
        var replies = new HashSet<DiscoveredCamera>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            using var socket = new UdpClient(new IPEndPoint(localAddress, 0));
            if (destination.Equals(Multicast))
            {
                socket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, localAddress.GetAddressBytes());
                socket.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 1);
            }

            string messageId = $"urn:uuid:{Guid.NewGuid()}";
            byte[] probe = Encoding.UTF8.GetBytes($"""
                <s:Envelope xmlns:s="{Soap}" xmlns:a="{Addressing}" xmlns:d="{Discovery}">
                  <s:Header>
                    <a:MessageID>{messageId}</a:MessageID>
                    <a:To>urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>
                    <a:Action>{Discovery}/Probe</a:Action>
                  </s:Header>
                  <s:Body><d:Probe /></s:Body>
                </s:Envelope>
                """);
            await socket.SendAsync(probe, destination, timeout.Token);
            while (!timeout.IsCancellationRequested)
            {
                var response = await socket.ReceiveAsync(timeout.Token);
                foreach (var camera in ReadMatches(response, messageId))
                    replies.Add(camera);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        catch (SocketException) { /* Interfaces can disappear while probing. */ }
        cancellationToken.ThrowIfCancellationRequested();
        return Collect(replies);
    }

    private static DiscoveredCamera[] Collect(IEnumerable<DiscoveredCamera> cameras)
        => cameras.GroupBy(camera => (camera.DeviceId, camera.Ip))
            .Select(group => group.FirstOrDefault(camera => camera.Name != null) ?? group.First())
            .OrderBy(camera => camera.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(camera => camera.Ip, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<DiscoveredCamera> ReadMatches(UdpReceiveResult response, string messageId)
    {
        XElement envelope;
        try
        {
            using var stream = new MemoryStream(response.Buffer);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            envelope = XElement.Load(reader);
        }
        catch (XmlException) { yield break; }

        var header = envelope.Element(Soap + "Header");
        if (envelope.Name != Soap + "Envelope" ||
            (string?)header?.Element(Addressing + "RelatesTo") != messageId ||
            (string?)header?.Element(Addressing + "Action") != $"{Discovery}/ProbeMatches")
            yield break;

        var matches = envelope.Element(Soap + "Body")?.Element(Discovery + "ProbeMatches");
        if (matches == null) yield break;

        foreach (var match in matches.Elements(Discovery + "ProbeMatch"))
        {
            string? id = ((string?)match.Element(Addressing + "EndpointReference")?.Element(Addressing + "Address"))?.Trim();
            if (string.IsNullOrEmpty(id)) continue;

            var scopes = Words((string?)match.Element(Discovery + "Scopes"));
            var types = match.Element(Discovery + "Types");
            bool isOnvif = scopes.Any(scope => scope.StartsWith(OnvifScope, StringComparison.OrdinalIgnoreCase)) ||
                Words(types?.Value).Any(type =>
                {
                    int colon = type.IndexOf(':');
                    return colon > 0 && types!.GetNamespaceOfPrefix(type[..colon])?.NamespaceName
                        .StartsWith("http://www.onvif.org/", StringComparison.Ordinal) == true;
                });
            if (!isOnvif) continue;

            const string nameScope = OnvifScope + "name/";
            string? name = scopes.FirstOrDefault(scope => scope.StartsWith(nameScope, StringComparison.OrdinalIgnoreCase));
            // The UDP response identifies the reachable local interface. Use
            // its IPv4 address for CGI, independent of the ONVIF service URL.
            yield return new(NormalizeDeviceId(id), response.RemoteEndPoint.Address.ToString(),
                name == null ? null : Uri.UnescapeDataString(name[nameScope.Length..]));
        }
    }

    private static string[] Words(string? value)
        => value?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? [];

    private static string NormalizeDeviceId(string id)
    {
        string value = id.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase) ? id[9..]
            : id.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase) ? id[5..] : id;
        return Guid.TryParse(value, out var uuid) ? $"urn:uuid:{uuid}" : id;
    }
}
