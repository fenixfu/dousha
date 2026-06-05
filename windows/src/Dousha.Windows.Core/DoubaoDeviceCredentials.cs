namespace Dousha.Windows.Core;

public sealed record DoubaoDeviceCredentials(
    string DeviceId,
    string InstallId,
    string Cdid,
    string Openudid,
    string Clientudid,
    string Token);
