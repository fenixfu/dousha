namespace Dousha.Windows.Core;

public static class TrayStatusFormatter
{
    public static string Format(DictationStatus status)
    {
        return status switch
        {
            DictationStatus.Idle => "Idle",
            DictationStatus.Recording => "Recording",
            DictationStatus.Transcribing => "Transcribing",
            DictationStatus.Inserting => "Inserting",
            DictationStatus.Success => "Success",
            DictationStatus.Error => "Error",
            _ => status.ToString()
        };
    }
}
