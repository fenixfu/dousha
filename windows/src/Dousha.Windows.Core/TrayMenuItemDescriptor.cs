namespace Dousha.Windows.Core;

public sealed record TrayMenuItemDescriptor(
    string Text,
    bool Enabled,
    TrayMenuCommand? Command = null);
