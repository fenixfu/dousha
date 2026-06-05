using Dousha.Windows.App;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var lifetime = new TrayApplicationLifetime();
        Application.Run(lifetime.Context);
    }
}
