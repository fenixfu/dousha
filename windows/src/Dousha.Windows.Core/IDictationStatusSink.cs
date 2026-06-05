namespace Dousha.Windows.Core;

public interface IDictationStatusSink
{
    void StatusChanged(DictationStatus status);
}
