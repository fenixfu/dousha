# Clipboard paste insertion

The Windows MVP will insert dictated text by writing it to the clipboard and simulating a paste command, without restoring the previous clipboard contents. This mirrors the macOS behavior and avoids timing races where the target application processes paste after the clipboard has already been restored, which would paste stale content instead of the dictated transcript.
