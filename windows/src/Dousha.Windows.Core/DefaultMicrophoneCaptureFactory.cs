namespace Dousha.Windows.Core;

public sealed class DefaultMicrophoneCaptureFactory : IDictationCaptureFactory
{
    public static AudioCaptureFormat CaptureFormat { get; } = new(16000, 16, 1);

    public IDictationCapture Create()
    {
        return new DefaultMicrophoneCapture(new WinMmDefaultMicrophoneInput(CaptureFormat));
    }
}
