namespace Dousha.Windows.Tests;

internal sealed class TestWorkspace : IDisposable
{
    private TestWorkspace(string root)
    {
        Root = root;
        ExecutableDirectory = Path.Combine(root, "portable");
        UserDataRoot = Path.Combine(root, "userdata");
        Directory.CreateDirectory(ExecutableDirectory);
        Directory.CreateDirectory(UserDataRoot);
    }

    public string Root { get; }

    public string ExecutableDirectory { get; }

    public string UserDataRoot { get; }

    public static TestWorkspace Create()
    {
        return new TestWorkspace(Path.Combine(Path.GetTempPath(), "Dousha.Windows.Tests", Guid.NewGuid().ToString("N")));
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
