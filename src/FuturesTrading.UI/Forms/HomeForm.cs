namespace FuturesTrading.UI.Forms;

/// <summary>
/// Pantalla de arranque (spec 0002). Vacía a propósito: es la base sobre la que se construirán
/// las siguientes pantallas mientras el login (spec 0001) está en pausa.
/// </summary>
public sealed class HomeForm : Form
{
    public HomeForm()
    {
        Name = "HomeForm";
        Text = "Futures Trading — Home";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(800, 500);
    }
}
