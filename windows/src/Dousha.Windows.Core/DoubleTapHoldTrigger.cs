namespace Dousha.Windows.Core;

public sealed class DoubleTapHoldTrigger
{
    private readonly TriggerSettings _settings;
    private DateTimeOffset? _armedAt;
    private bool _triggerKeyDown;
    private bool _otherKeyPressedDuringCurrentHold;
    private bool _recording;

    public DoubleTapHoldTrigger(TriggerSettings settings)
    {
        _settings = settings;
    }

    public TriggerResult Handle(TriggerKeyEvent keyEvent)
    {
        if (keyEvent.Kind is TriggerKeyEventKind.OtherKeyDown)
        {
            _armedAt = null;
            _otherKeyPressedDuringCurrentHold = true;
            return TriggerResult.None;
        }

        if (keyEvent.Key != _settings.TriggerKey)
        {
            return TriggerResult.None;
        }

        return keyEvent.Kind switch
        {
            TriggerKeyEventKind.KeyDown => HandleTriggerKeyDown(keyEvent.Timestamp),
            TriggerKeyEventKind.KeyUp => HandleTriggerKeyUp(keyEvent.Timestamp),
            _ => TriggerResult.None
        };
    }

    private TriggerResult HandleTriggerKeyDown(DateTimeOffset timestamp)
    {
        if (_triggerKeyDown)
        {
            return TriggerResult.None;
        }

        _triggerKeyDown = true;
        _otherKeyPressedDuringCurrentHold = false;

        if (_armedAt is not { } armedAt)
        {
            return TriggerResult.None;
        }

        if (timestamp - armedAt > TimeSpan.FromMilliseconds(_settings.DoubleTapWindowMilliseconds))
        {
            _armedAt = null;
            return TriggerResult.None;
        }

        _armedAt = null;
        _recording = true;
        return TriggerResult.WithCommand(TriggerCommand.StartDictation);
    }

    private TriggerResult HandleTriggerKeyUp(DateTimeOffset timestamp)
    {
        if (!_triggerKeyDown)
        {
            return TriggerResult.None;
        }

        _triggerKeyDown = false;

        if (_recording)
        {
            _recording = false;
            return TriggerResult.WithCommand(TriggerCommand.StopDictation);
        }

        if (!_otherKeyPressedDuringCurrentHold)
        {
            _armedAt = timestamp;
        }

        _otherKeyPressedDuringCurrentHold = false;
        return TriggerResult.None;
    }
}
