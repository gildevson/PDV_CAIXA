using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PDV_CAIXA.Views {
    public partial class MovimentacaoWindow : Window {
        public string  Descricao { get; private set; } = "";
        public decimal Valor     { get; private set; }

        public MovimentacaoWindow(string tipo) {
            InitializeComponent();

            if (tipo == "entrada") {
                AplicarTema(
                    accentTop:    Color.FromRgb(0x50, 0xFA, 0x7B),
                    accentBot:    Color.FromRgb(0x1A, 0x80, 0x40),
                    circleTop:    Color.FromRgb(0x1A, 0x6A, 0x30),
                    circleBot:    Color.FromRgb(0x0A, 0x3A, 0x18),
                    glowColor:    Color.FromRgb(0x50, 0xFA, 0x7B),
                    pillBg:       Color.FromArgb(0x35, 0x1A, 0x6A, 0x30),
                    pillFg:       Color.FromRgb(0x50, 0xFA, 0x7B),
                    valorFg:      Color.FromRgb(0x50, 0xFA, 0x7B),
                    valorBdTop:   Color.FromArgb(0x70, 0x50, 0xFA, 0x7B),
                    valorBdBot:   Color.FromArgb(0x90, 0x1A, 0x6A, 0x30),
                    valorBgTop:   Color.FromRgb(0x0A, 0x18, 0x0F),
                    valorBgBot:   Color.FromRgb(0x0D, 0x1E, 0x14),
                    rsFg:         Color.FromRgb(0x30, 0x55, 0x40),
                    barraTipo:    "NOVA ENTRADA",
                    tituloTexto:  "Nova Entrada",
                    subtitulo:    "ENTRADA DE CAIXA",
                    icone:        "⬆",
                    iconeDecor:   "💰",
                    btnLabel:     "✓   Registrar Entrada"
                );
            } else {
                AplicarTema(
                    accentTop:    Color.FromRgb(0xFF, 0x55, 0x55),
                    accentBot:    Color.FromRgb(0x80, 0x18, 0x18),
                    circleTop:    Color.FromRgb(0x6A, 0x1A, 0x1A),
                    circleBot:    Color.FromRgb(0x3A, 0x0A, 0x0A),
                    glowColor:    Color.FromRgb(0xFF, 0x55, 0x55),
                    pillBg:       Color.FromArgb(0x35, 0x6A, 0x1A, 0x1A),
                    pillFg:       Color.FromRgb(0xFF, 0x55, 0x55),
                    valorFg:      Color.FromRgb(0xFF, 0x55, 0x55),
                    valorBdTop:   Color.FromArgb(0x70, 0xFF, 0x55, 0x55),
                    valorBdBot:   Color.FromArgb(0x90, 0x6A, 0x18, 0x18),
                    valorBgTop:   Color.FromRgb(0x18, 0x0A, 0x0A),
                    valorBgBot:   Color.FromRgb(0x1E, 0x0D, 0x0D),
                    rsFg:         Color.FromRgb(0x55, 0x30, 0x30),
                    barraTipo:    "NOVA SAÍDA",
                    tituloTexto:  "Nova Saída",
                    subtitulo:    "SAÍDA DE CAIXA",
                    icone:        "⬇",
                    iconeDecor:   "💸",
                    btnLabel:     "✓   Registrar Saída"
                );

                // botão vermelho via Tag
                btnConfirmar.Tag = "saida";
            }

            txtDescricao.Focus();
        }

        private void AplicarTema(
            Color accentTop, Color accentBot,
            Color circleTop, Color circleBot, Color glowColor,
            Color pillBg, Color pillFg,
            Color valorFg, Color valorBdTop, Color valorBdBot, Color valorBgTop, Color valorBgBot,
            Color rsFg,
            string barraTipo, string tituloTexto, string subtitulo,
            string icone, string iconeDecor, string btnLabel)
        {
            // textos
            txtBarraTipo.Text    = barraTipo;
            txtTitulo.Text       = tituloTexto;
            txtSubtitulo.Text    = subtitulo;
            txtIcone.Text        = icone;
            txtIconeDecor.Text   = iconeDecor;
            btnConfirmar.Content = btnLabel;

            // accent bar
            bdAccent.Background = new LinearGradientBrush(accentTop, accentBot, 90);

            // círculo ícone
            bdIconCircle.Background = new LinearGradientBrush(circleTop, circleBot, 45);
            bdIconCircle.Effect = new DropShadowEffect {
                Color = glowColor, BlurRadius = 26, ShadowDepth = 0, Opacity = 0.65
            };

            // pill
            bdPill.Background = new SolidColorBrush(pillBg);
            txtSubtitulo.Foreground = new SolidColorBrush(pillFg);

            // campo valor
            txtValor.Foreground  = new SolidColorBrush(valorFg);
            txtValor.CaretBrush  = new SolidColorBrush(valorFg);
            txtValor.Effect = new DropShadowEffect {
                Color = valorFg, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.50
            };
            txtRS.Foreground = new SolidColorBrush(rsFg);

            bdValor.BorderBrush = new LinearGradientBrush(valorBdTop, valorBdBot, 0);
            bdValor.Background  = new LinearGradientBrush(valorBgTop, valorBgBot, 0);
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e) {
            if (string.IsNullOrWhiteSpace(txtDescricao.Text)) {
                MessageBox.Show("Informe a descrição.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDescricao.Focus();
                return;
            }
            var texto = txtValor.Text.Replace(",", ".").Trim();
            if (!decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor <= 0) {
                MessageBox.Show("Informe um valor válido maior que zero.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtValor.Focus();
                return;
            }
            Descricao    = txtDescricao.Text.Trim();
            Valor        = valor;
            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void TxtValor_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"[\d,\.]");

        private void TxtDescricao_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) txtValor.Focus();
        }

        private void TxtValor_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) BtnConfirmar_Click(sender, e);
        }
    }
}
