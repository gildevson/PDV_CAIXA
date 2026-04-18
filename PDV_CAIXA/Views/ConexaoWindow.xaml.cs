using Npgsql;
using PDV_CAIXA.Config;
using System.Windows;
using System.Windows.Media;

namespace PDV_CAIXA.Views {
    public partial class ConexaoWindow : Window {

        public bool Salvo { get; private set; }

        public ConexaoWindow(string? mensagemErro = null) {
            InitializeComponent();
            CarregarConfigAtual();

            if (mensagemErro != null) {
                txtSubtitulo.Text = "⚠ Não foi possível conectar ao banco de dados. Verifique os dados abaixo.";
                txtSubtitulo.Foreground = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FFB86C"));
            }
        }

        private void CarregarConfigAtual() {
            try {
                var cs = AppConfig.ConnectionString;
                var pares = cs.Split(';', StringSplitOptions.RemoveEmptyEntries)
                              .Select(p => p.Split('=', 2))
                              .Where(p => p.Length == 2)
                              .ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim());

                txtHost.Text      = pares.GetValueOrDefault("host",     "localhost");
                txtPorta.Text     = pares.GetValueOrDefault("port",     "5432");
                txtBanco.Text     = pares.GetValueOrDefault("database", "");
                txtUsuario.Text   = pares.GetValueOrDefault("username", "postgres");
                txtSenha.Password = pares.GetValueOrDefault("password", "");
            } catch { }
        }

        // ── Testar: se OK salva e fecha, se falhou mostra o erro ─────
        private async void BtnTestar_Click(object sender, RoutedEventArgs e) {
            if (!ValidarCampos()) return;

            btnTestar.IsEnabled  = false;
            btnCancelar.IsEnabled = false;
            MostrarStatus("⏳", "Testando conexão...", "#2A2A3E", "#A0A0C0");

            var cs = MontarConnectionString();
            try {
                await Task.Run(() => {
                    using var conn = new NpgsqlConnection(cs);
                    conn.Open();
                    conn.Close();
                });

                // ✅ Conexão OK — salva automaticamente e fecha
                AppConfig.Salvar(
                    txtHost.Text.Trim(),
                    txtPorta.Text.Trim(),
                    txtBanco.Text.Trim(),
                    txtUsuario.Text.Trim(),
                    txtSenha.Password);

                MostrarStatus("✅", "Conexão OK! Configuração salva. Abrindo o sistema...", "#0D2A1A", "#50FA7B");
                Salvo = true;

                await Task.Delay(1200);
                Close();

            } catch (Exception ex) {
                var msg = ex.InnerException?.Message ?? ex.Message;
                MostrarStatus("❌", $"Falha: {msg}", "#2A0D0D", "#FF5555");
                btnTestar.IsEnabled   = true;
                btnCancelar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) {
            Salvo = false;
            Close();
        }

        private string MontarConnectionString() =>
            $"Host={txtHost.Text.Trim()};Port={txtPorta.Text.Trim()};" +
            $"Database={txtBanco.Text.Trim()};Username={txtUsuario.Text.Trim()};" +
            $"Password={txtSenha.Password}";

        private bool ValidarCampos() {
            if (string.IsNullOrWhiteSpace(txtHost.Text)   ||
                string.IsNullOrWhiteSpace(txtPorta.Text)  ||
                string.IsNullOrWhiteSpace(txtBanco.Text)  ||
                string.IsNullOrWhiteSpace(txtUsuario.Text)) {
                MostrarStatus("⚠", "Preencha todos os campos obrigatórios.", "#2A2A0D", "#FFB86C");
                return false;
            }
            return true;
        }

        private void MostrarStatus(string icone, string msg, string corFundo, string corTexto) {
            borderStatus.Visibility = Visibility.Visible;
            borderStatus.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(corFundo));
            txtStatusIcone.Text     = icone;
            txtStatusMsg.Text       = msg;
            txtStatusMsg.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(corTexto));
        }
    }
}
