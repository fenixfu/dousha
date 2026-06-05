namespace Dousha.Windows.Core;

public sealed record NonBlockingErrorFeedback(
    DiagnosticArea Area,
    string EventName,
    DateTimeOffset OccurredAt);
