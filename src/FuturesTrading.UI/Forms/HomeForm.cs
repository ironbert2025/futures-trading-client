using FuturesTrading.UI.Controls;

namespace FuturesTrading.UI.Forms;

/// <summary>
/// Pantalla de arranque. Contiene las secciones principales como pestañas.
/// </summary>
public sealed class HomeForm : Form
{
    private readonly TabControl _tabMain = new();
    private readonly TabPage _tabCharts = new();
    private readonly TabPage _tabSettings = new();

    public HomeForm(ChartsGridPanel chartsGrid)
    {
        Name = "HomeForm";
        Text = "Futures Trading — Home";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1280, 780);

        chartsGrid.Dock = DockStyle.Fill;

        _tabCharts.Name = "tabCharts";
        _tabCharts.Text = "Charts";
        _tabCharts.Controls.Add(chartsGrid);

        _tabSettings.Name = "tabSettings";
        _tabSettings.Text = "Settings";

        _tabMain.Name = "tabMain";
        _tabMain.Dock = DockStyle.Fill;
        _tabMain.TabPages.AddRange([_tabCharts, _tabSettings]);

        Controls.Add(_tabMain);
    }
}
