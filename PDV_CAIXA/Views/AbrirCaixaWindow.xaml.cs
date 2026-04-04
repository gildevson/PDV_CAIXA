using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace PDV_CAIXA.Views {
    public partial class AbrirCaixaWindow : Window {
        public decimal SaldoInicial { get; private set; }

        private readonly CultureInfo _ptBR = new("pt-BR");

        public AbrirCaixaWindow() {
            InitializeComponent();
            txtSaldo.SelectAll();
            txtSaldo.Focus();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e) {
            var texto = txtSaldo.Text.Replace(",", ".").Trim();
            if (!decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor < 0) {
                MessageBox.Show("Informe um valor válido.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SaldoInicial = valor;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void TxtSaldo_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"[\d,\.]");

        private void TxtSaldo_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) BtnConfirmar_Click(sender, e);
        }

        private void txtSaldo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {

        }
    }
}
