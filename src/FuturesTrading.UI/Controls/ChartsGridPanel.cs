using FuturesTrading.Application.Charts;
using Microsoft.Extensions.Options;

namespace FuturesTrading.UI.Controls;

/// <summary>
/// Pestaña Charts: una barra de herramientas y debajo una cuadrícula de 2×2 gráficos.
/// Siempre reserva los 4 espacios: los que no tienen símbolo configurado quedan inactivos hasta
/// que se define en appsettings.json. Si se configuran más de 4, el resto se ignora.
/// </summary>
public sealed class ChartsGridPanel : UserControl
{
    private const int Columns = 2;
    private const int Rows = 2;

    private readonly List<CandleChartPanel> _panels = new();
    private readonly Button _btnDzSz = new();
    private bool _dzSzArmed;

    public ChartsGridPanel(ICandleFeed feed, IOptions<ChartsSettings> options)
    {
        var configured = options.Value.Panels;

        var grid = new TableLayoutPanel
        {
            Name = "chartsGrid",
            Dock = DockStyle.Fill,
            ColumnCount = Columns,
            RowCount = Rows,
        };
        for (var c = 0; c < Columns; c++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Columns));
        for (var r = 0; r < Rows; r++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / Rows));

        for (var i = 0; i < Columns * Rows; i++)
        {
            var settings = i < configured.Count ? configured[i] : new ChartSettings();
            var panel = new CandleChartPanel(feed, settings, slot: i + 1)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(2),
            };
            _panels.Add(panel);
            grid.Controls.Add(panel, i % Columns, i / Columns);
        }

        _btnDzSz.Name = "btnDzSz";
        _btnDzSz.Text = "DZ/SZ";
        _btnDzSz.Location = new Point(6, 4);
        _btnDzSz.Size = new Size(70, 26);
        _btnDzSz.Click += (_, _) => ToggleDzSz();

        var toolbar = new Panel
        {
            Name = "chartsToolbar",
            Dock = DockStyle.Top,
            Height = 34,
        };
        toolbar.Controls.Add(_btnDzSz);

        // El orden de Add importa en WinForms: el control Fill va primero, el Top después.
        Controls.Add(grid);
        Controls.Add(toolbar);
    }

    // Un solo botón arma (o desarma) el dibujo de zonas en los 4 gráficos a la vez; cada click
    // dibuja en el gráfico donde se hizo. El estado vive aquí y se reenvía a cada panel.
    private void ToggleDzSz()
    {
        _dzSzArmed = !_dzSzArmed;
        _btnDzSz.BackColor = _dzSzArmed ? Color.LightGreen : SystemColors.Control;
        foreach (var panel in _panels) panel.SetDzSzArmed(_dzSzArmed);
    }
}
