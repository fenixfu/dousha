namespace Dousha.Windows.Core;

public interface IPasteCommandSender
{
    Task SendPasteAsync(CancellationToken cancellationToken = default);
}
