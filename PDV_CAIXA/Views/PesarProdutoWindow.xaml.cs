using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Views {
    public partial class PesarProdutoWindow : Window {
        private readonly decimal _precoPorKg;

        public decimal PesoKg { get; private set; }
        public decimal Total  { get; private set; }

        public PesarProdutoWindow(ProdutoViewModel produto) {
            InitializeComponent();
            _precoPorKg         = produto.PrecoComDesconto;
            txtNomeProduto.Text = produto.Nome;
            txtPrecoPorKg.Text  = $"R$ {_precoPorKg.ToString("N2", new CultureInfo("pt-BR"))} / kg";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            txtKg.Focus();
            txtKg.SelectAll();
        }

        private void Peso_GotFocus(object sender, RoutedEventArgs e) {
            if (sender is not TextBox tb) return;
            if (tb.Text == "0") tb.SelectAll();

            // destaca a borda do campo ativo
            var corAtiva  = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7C83FF"));
            var corNormal = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A3A55"));

            borderKg.BorderBrush     = tb == txtKg     ? corAtiva : corNormal;
            borderGramas.BorderBrush = tb == txtGramas ? corAtiva : corNormal;
        }

        private void Peso_TextChanged(object sender, TextChangedEventArgs e)
            => Recalcular();

        private void Recalcular() {
            if (txtKg == null || txtGramas == null || borderKg == null ||
                txtErroPeso == null || borderTotal == null || btnConfirmar == null) return;

            int.TryParse(txtKg.Text,     out var kg);
            int.TryParse(txtGramas.Text, out var gramas);

            if (gramas > 999) {
                txtErroPeso.Text       = "Gramas deve ser entre 0 e 999.";
                txtErroPeso.Visibility = Visibility.Visible;
                borderTotal.Visibility = Visibility.Collapsed;
                btnConfirmar.IsEnabled = false;
                return;
            }

            var pesoTotal = kg + (gramas / 1000m);

            if (pesoTotal > 0) {
                PesoKg  = pesoTotal;
                Total   = Math.Round(_precoPorKg * pesoTotal, 2);

                var ptBR        = new CultureInfo("pt-BR");
                var pesoTexto   = pesoTotal.ToString("N3", ptBR);
                txtResumo.Text  = $"{pesoTexto} kg  ×  {_precoPorKg.ToString("C2", ptBR)}/kg";
                txtTotal.Text   = Total.ToString("C2", ptBR);

                txtErroPeso.Visibility = Visibility.Collapsed;
                borderTotal.Visibility = Visibility.Visible;
                btnConfirmar.IsEnabled = true;
            } else {
                borderTotal.Visibility = Visibility.Collapsed;
                btnConfirmar.IsEnabled = false;
            }
        }

        private void Peso_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (sender == txtKg) {
                    txtGramas.Focus();
                    txtGramas.SelectAll();
                    e.Handled = true;
                } else if (btnConfirmar.IsEnabled) {
                    Confirmar();
                    e.Handled = true;
                }
            }
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
            => Confirmar();

        private void Confirmar() {
            if (PesoKg <= 0) {
                txtErroPeso.Text       = "Informe um peso válido.";
                txtErroPeso.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }
    }
}
