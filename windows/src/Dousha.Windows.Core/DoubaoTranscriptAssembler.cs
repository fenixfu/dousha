namespace Dousha.Windows.Core;

public sealed class DoubaoTranscriptAssembler
{
    private readonly IDiagnosticLog _diagnosticLog;
    private readonly List<string> _committedSegments = [];
    private string _currentInterim = "";

    public DoubaoTranscriptAssembler(IDiagnosticLog diagnosticLog)
    {
        _diagnosticLog = diagnosticLog;
    }

    public string Text => string.Concat(_committedSegments) + _currentInterim;

    public void Apply(DoubaoRecognitionEvent recognitionEvent)
    {
        if (string.IsNullOrEmpty(recognitionEvent.Text))
        {
            return;
        }

        var text = recognitionEvent.Text;
        var isCommit = recognitionEvent.IsFinalized;
        var looksLikeNewUtterance = !isCommit
            && !string.IsNullOrEmpty(_currentInterim)
            && text.Length * 2 < _currentInterim.Length
            && !_currentInterim.StartsWith(text, StringComparison.Ordinal);

        if (looksLikeNewUtterance)
        {
            var rescued = _currentInterim;
            _committedSegments.Add(rescued);
            _currentInterim = text;
            _diagnosticLog.Lifecycle($"doubao.transcript.segment_rescued textLength={rescued.Length} newTextLength={text.Length} segments={_committedSegments.Count}");
            return;
        }

        if (isCommit)
        {
            _committedSegments.Add(text);
            _currentInterim = "";
            _diagnosticLog.Lifecycle($"doubao.transcript.segment_final textLength={text.Length} segments={_committedSegments.Count}");
            return;
        }

        _currentInterim = text;
    }
}
