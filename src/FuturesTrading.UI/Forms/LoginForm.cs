using FuturesTrading.Application.Auth;

namespace FuturesTrading.UI.Forms;

/// <summary>
/// Formulario "tonto": solo captura input, llama a IAuthService, y muestra el resultado.
/// Nada de lógica de red o de negocio vive aquí — eso está en Application/Infrastructure.
/// </summary>
public sealed class LoginForm : Form
{
    private readonly IAuthService _authService;

    private readonly TextBox _txtUsername = new();
    private readonly TextBox _txtPassword = new();
    private readonly Button _btnLogin = new();
    private readonly Label _lblError = new();

    public LoginForm(IAuthService authService)
    {
        _authService = authService;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Futures Trading — Ingresar";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ClientSize = new Size(320, 180);

        var lblUsername = new Label { Text = "Usuario:", Location = new Point(20, 20), AutoSize = true };
        _txtUsername.Location = new Point(110, 17);
        _txtUsername.Width = 180;

        var lblPassword = new Label { Text = "Password:", Location = new Point(20, 55), AutoSize = true };
        _txtPassword.Location = new Point(110, 52);
        _txtPassword.Width = 180;
        _txtPassword.UseSystemPasswordChar = true;

        _lblError.Location = new Point(20, 85);
        _lblError.Size = new Size(280, 40);
        _lblError.ForeColor = Color.Firebrick;
        _lblError.Visible = false;

        _btnLogin.Text = "Ingresar";
        _btnLogin.Location = new Point(110, 130);
        _btnLogin.Width = 100;
        _btnLogin.Click += BtnLogin_Click;

        Controls.AddRange([lblUsername, _txtUsername, lblPassword, _txtPassword, _lblError, _btnLogin]);
        AcceptButton = _btnLogin;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        _lblError.Visible = false;
        _btnLogin.Enabled = false;

        try
        {
            var result = await _authService.LoginAsync(_txtUsername.Text, _txtPassword.Text);

            if (!result.Success)
            {
                _lblError.Text = result.ErrorMessage;
                _lblError.Visible = true;
                return;
            }

            var mainForm = new MainForm(result.User!);
            mainForm.FormClosed += (_, _) => Close();
            mainForm.Show();
            Hide();
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }
}
