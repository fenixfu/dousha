namespace Dousha.Windows.Core;

public interface ITextInsertion : IAsyncDisposable
{
    Task InsertAsync(string text, CancellationToken cancellationToken = default);
}
