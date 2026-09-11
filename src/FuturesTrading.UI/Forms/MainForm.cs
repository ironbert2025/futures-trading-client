using FuturesTrading.Domain;

namespace FuturesTrading.UI.Forms;

/// <summary>
/// Placeholder de la ventana principal — solo confirma que el login funcionó.
/// Aquí es donde, en iteraciones futuras, se conectará el resto de la app
/// (setups, integraciones con NinjaTrader/ThinkorSwim, etc.).
/// </summary>
public sealed class MainForm : Form
{
    public MainForm(AuthenticatedUser user)
    {
        Text = "Futures Trading";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(400, 200);

        var label = new Label
        {
            Text = $"Bienvenido, {user.Username}",
            AutoSize = true,
            Location = new Point(20, 20),
            Font = new Font(Font.FontFamily, 14),
        };

        Controls.Add(label);
    }
}
