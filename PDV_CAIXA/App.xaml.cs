using Dapper;
using Npgsql;
using PDV_CAIXA.Config;
using PDV_CAIXA.Views;
using System.Windows;
using System.Windows.Threading;

namespace PDV_CAIXA {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            DefaultTypeMap.MatchNamesWithUnderscores = true;

            DispatcherUnhandledException += (s, ex) => {
                MessageBox.Show(
                    "Erro inesperado:\n\n" + ex.Exception.Message +
                    "\n\nLocalização:\n" + ex.Exception.StackTrace,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            // Inicia o fluxo de forma assíncrona — não bloqueia a thread principal
            Dispatcher.BeginInvoke(IniciarApp);
        }

        private async void IniciarApp() {
            var (ok, erro) = await Task.Run(TestarConexao);

            if (!ok) {
                var janela = new ConexaoWindow(erro);
                janela.ShowDialog();

                if (!janela.Salvo) {
                    Shutdown();
                    return;
                }

                // Após salvar, testa novamente
                (ok, erro) = await Task.Run(TestarConexao);
                if (!ok) {
                    // Ainda falhou — reabre a tela
                    IniciarApp();
                    return;
                }
            }

            new LoginWindow().Show();
        }

        internal static (bool ok, string? erro) TestarConexao() {
            try {
                using var conn = new NpgsqlConnection(AppConfig.ConnectionString);
                conn.Open();
                conn.Close();
                return (true, null);
            } catch (Exception ex) {
                return (false, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
