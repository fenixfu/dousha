namespace Dousha.Windows.Core;

public sealed class ApplicationExitCoordinator
{
    private bool _exitRequested;

    public event EventHandler? ExitRequested;

    public bool IsExitRequested => _exitRequested;

    public void RequestExit()
    {
        if (_exitRequested)
        {
            return;
        }

        _exitRequested = true;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
