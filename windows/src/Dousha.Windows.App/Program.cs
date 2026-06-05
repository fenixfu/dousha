using Dousha.Windows.App;
using System.Windows.Forms;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

using var lifetime = new TrayApplicationLifetime();
Application.Run(lifetime.Context);
