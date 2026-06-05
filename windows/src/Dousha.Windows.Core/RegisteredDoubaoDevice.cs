namespace Dousha.Windows.Core;

public sealed record RegisteredDoubaoDevice(
    string DeviceId,
    string InstallId,
    string Cdid,
    string Openudid,
    string Clientudid);
