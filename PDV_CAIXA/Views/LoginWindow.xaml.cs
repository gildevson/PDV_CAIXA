using System.Windows;
using PDV_CAIXA.Services;

namespace PDV_CAIXA.Views {
    public partial class LoginWindow : Window {
        private readonly AuthService _authService = new();

        public LoginWindow() {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e) {
            txtStatus.Text = string.Empty;
            btnLogin.IsEnabled = false;

            var nome  = txtUsuario.Text?.Trim() ?? string.Empty;
            var senha = pwdSenha.Password ?? string.Empty;

            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(senha)) {
                txtStatus.Text = "Informe usuário e senha.";
                btnLogin.IsEnabled = true;
                return;
            }

            try {
                var usuario = await _authService.ValidateCredentialsAsync(nome, senha);
                if (usuario != null) {
                    var main = new MainWindow(usuario);
                    main.Show();
                    this.Close();
                } else {
                    txtStatus.Text = "Credenciais inválidas.";
                }
            } catch (System.Exception ex) {
                txtStatus.Text = "Erro ao conectar: " + ex.Message;
            } finally {
                btnLogin.IsEnabled = true;
            }
        }
    }
}
