using Dapper;
using System.Windows;
using System.Windows.Threading;

namespace PDV_CAIXA {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            // Mapeia automaticamente snake_case (codigo_barras) → PascalCase (CodigoBarras)
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            DispatcherUnhandledException += (s, ex) => {
                MessageBox.Show(
                    "Erro inesperado:\n\n" + ex.Exception.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };
        }
    }
}
