using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PDV_CAIXA.Views {

    public class PagamentoItem {
        public string  Forma { get; set; } = "";
        public decimal Valor { get; set; }

        public string FormaTexto => Forma switch {
            "pix"     => "PIX",
            "dinheiro"=> "Dinheiro",
            "credito" => "Crédito",
            "debito"  => "Débito",
            _         => Forma
        };

        public string Icone => Forma switch {
            "pix"      => "📱",
            "dinheiro" => "💵",
            _          => "💳"
        };

        public string VistoCor => Forma switch {
            "pix"      => "#00C853",
            "dinheiro" => "#448AFF",
            "credito"  => "#FFB86C",
            "debito"   => "#7C83FF",
            _          => "#50FA7B"
        };
    }

    public partial class PagamentoWindow : Window {

        // ── Resultado exposto ao caller ───────────────────────────────
        public List<PagamentoItem> Pagamentos       { get; private set; } = new();
        public decimal             Troco            { get; private set; }
        public string              FormaSelecionada { get; private set; } = "";

        // ── Estado interno ────────────────────────────────────────────
        private readonly decimal      _total;
        private readonly CultureInfo  _ptBR      = new("pt-BR");
        private string                _metodoAtual = "dinheiro";

        // Referências dos 4 cards de método
        private Dictionary<string, Border> _metodoCards = new();

        // Cores dos métodos
        private static readonly Dictionary<string, string> _coresMetodo = new() {
            ["pix"]      = "#00C853",
            ["dinheiro"] = "#448AFF",
            ["credito"]  = "#FFB86C",
            ["debito"]   = "#7C83FF"
        };

        // ── Valores calculados ────────────────────────────────────────
        private decimal TotalPago    => Pagamentos.Sum(p => p.Valor);
        private decimal Faltando     => Math.Max(0, _total - TotalPago);
        private decimal TrocoAtual   => Math.Max(0, TotalPago - _total);

        public PagamentoWindow(decimal total) {
            InitializeComponent();
            _total = total;

            txtTotalCompra.Text = total.ToString("C2", _ptBR);

            _metodoCards = new Dictionary<string, Border> {
                ["pix"]      = bdMetodoPix,
                ["dinheiro"] = bdMetodoDinheiro,
                ["credito"]  = bdMetodoCredito,
                ["debito"]   = bdMetodoDebito
            };

            SelecionarMetodo("dinheiro");
            AtualizarUI();
        }

        // ── Seleção de método ─────────────────────────────────────────

        private void BdMetodo_Click(object sender, MouseButtonEventArgs e) {
            var tag = ((Border)sender).Tag as string ?? "dinheiro";
            SelecionarMetodo(tag);
        }

        private void SelecionarMetodo(string metodo) {
            _metodoAtual = metodo;

            foreach (var (key, bd) in _metodoCards) {
                var cor = ColorFromHex(_coresMetodo[key]);
                if (key == metodo) {
                    bd.BorderBrush  = new SolidColorBrush(cor);
                    bd.Background   = new SolidColorBrush(Color.FromArgb(30, cor.R, cor.G, cor.B));
                    // Atualiza cor do texto do label
                    if (bd.Child is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(cor);
                } else {
                    bd.BorderBrush  = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3E));
                    bd.Background   = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
                    if (bd.Child is StackPanel sp && sp.Children.Count > 1 && sp.Children[1] is TextBlock tb)
                        tb.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xC0));
                }
            }

            // Pré-preenche o valor com o que falta
            if (Faltando > 0)
                txtValorAdicionar.Text = Faltando.ToString("F2", _ptBR).Replace(".", ",");
            else
                txtValorAdicionar.Text = "";
        }

        // ── Adicionar pagamento ───────────────────────────────────────

        private void BtnAdicionar_Click(object sender, RoutedEventArgs e) {
            var texto = txtValorAdicionar.Text.Replace(",", ".").Trim();

            if (!decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) || valor <= 0) {
                FlashBorda();
                return;
            }

            Pagamentos.Add(new PagamentoItem { Forma = _metodoAtual, Valor = valor });

            txtValorAdicionar.Text = "";
            AtualizarUI();

            // Sugere próximo método diferente se ainda falta pagar
            if (Faltando > 0)
                txtValorAdicionar.Text = Faltando.ToString("F2", _ptBR).Replace(".", ",");
        }

        // ── Remover item da lista ─────────────────────────────────────

        private void RemoverPagamento(PagamentoItem item) {
            Pagamentos.Remove(item);
            AtualizarUI();

            if (Faltando > 0)
                txtValorAdicionar.Text = Faltando.ToString("F2", _ptBR).Replace(".", ",");
        }

        // ── Atualizar toda a UI ───────────────────────────────────────

        private void AtualizarUI() {
            // Pago
            txtPago.Text = TotalPago.ToString("C2", _ptBR);

            // Faltando / Troco
            if (TrocoAtual > 0) {
                lblFaltandoTroco.Text    = "TROCO";
                txtFaltando.Text         = TrocoAtual.ToString("C2", _ptBR);
                txtFaltando.Foreground   = new SolidColorBrush(Color.FromRgb(0x50, 0xFA, 0x7B));
                bdTroco.Visibility       = Visibility.Visible;
                txtTroco.Text            = TrocoAtual.ToString("C2", _ptBR);
            } else if (Faltando > 0) {
                lblFaltandoTroco.Text    = "FALTANDO";
                txtFaltando.Text         = Faltando.ToString("C2", _ptBR);
                txtFaltando.Foreground   = new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));
                bdTroco.Visibility       = Visibility.Collapsed;
            } else {
                lblFaltandoTroco.Text    = "FALTANDO";
                txtFaltando.Text         = "R$ 0,00";
                txtFaltando.Foreground   = new SolidColorBrush(Color.FromRgb(0x50, 0xFA, 0x7B));
                bdTroco.Visibility       = Visibility.Collapsed;
            }

            // Habilitar confirmar
            btnConfirmar.IsEnabled = TotalPago >= _total && Pagamentos.Count > 0;

            // Reconstruir lista visual
            stackPagamentos.Children.Clear();

            if (Pagamentos.Count == 0) {
                var vazio = new TextBlock {
                    Text                = "Nenhum pagamento adicionado",
                    Foreground          = new SolidColorBrush(Color.FromRgb(0x44, 0x47, 0x5A)),
                    FontSize            = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 8, 0, 8)
                };
                stackPagamentos.Children.Add(vazio);
                return;
            }

            foreach (var item in Pagamentos.ToList()) {
                var cor = ColorFromHex(item.VistoCor);

                var bdVista = new Border {
                    Width           = 4,
                    Background      = new SolidColorBrush(cor),
                    CornerRadius    = new CornerRadius(3, 0, 0, 3)
                };

                var icone = new TextBlock {
                    Text                = item.Icone,
                    FontSize            = 16,
                    VerticalAlignment   = VerticalAlignment.Center,
                    Margin              = new Thickness(12, 0, 8, 0)
                };

                var forma = new TextBlock {
                    Text              = item.FormaTexto,
                    FontSize          = 13,
                    FontWeight        = FontWeights.SemiBold,
                    Foreground        = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var valor = new TextBlock {
                    Text              = item.Valor.ToString("C2", _ptBR),
                    FontSize          = 14,
                    FontWeight        = FontWeights.Bold,
                    Foreground        = new SolidColorBrush(cor),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin            = new Thickness(0, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var capturado = item;
                var btnRemover = new Border {
                    Background      = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                    Padding         = new Thickness(8, 0, 4, 0),
                    Cursor          = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child           = new TextBlock {
                        Text       = "×",
                        FontSize   = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x6F, 0x85))
                    }
                };
                btnRemover.MouseLeftButtonDown += (_, _) => RemoverPagamento(capturado);
                btnRemover.MouseEnter += (_, _) => {
                    if (btnRemover.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));
                };
                btnRemover.MouseLeave += (_, _) => {
                    if (btnRemover.Child is TextBlock t) t.Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x6F, 0x85));
                };

                var conteudo = new Grid();
                conteudo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                conteudo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                conteudo.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                conteudo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                conteudo.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn(icone,     0);
                Grid.SetColumn(forma,     1);
                Grid.SetColumn(valor,     2);
                Grid.SetColumn(btnRemover,3);

                conteudo.Children.Add(icone);
                conteudo.Children.Add(forma);
                conteudo.Children.Add(valor);
                conteudo.Children.Add(btnRemover);

                var linha = new Border {
                    Background      = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E)),
                    BorderBrush     = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x3E)),
                    BorderThickness = new Thickness(1),
                    CornerRadius    = new CornerRadius(8),
                    Margin          = new Thickness(0, 0, 0, 6),
                    Height          = 44,
                    ClipToBounds    = true
                };

                var linhaGrid = new Grid();
                linhaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                linhaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Grid.SetColumn(bdVista,  0);
                Grid.SetColumn(conteudo, 1);
                linhaGrid.Children.Add(bdVista);
                linhaGrid.Children.Add(conteudo);

                linha.Child = linhaGrid;
                stackPagamentos.Children.Add(linha);
            }
        }

        // ── Input ─────────────────────────────────────────────────────

        private void TxtValor_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = !Regex.IsMatch(e.Text, @"[\d,\.]");
        }

        private void TxtValor_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                BtnAdicionar_Click(sender, e);
        }

        // ── Confirmar / Cancelar ──────────────────────────────────────

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e) {
            Troco = TrocoAtual;

            // FormaSelecionada: tipo único ou "misto"
            var formas = Pagamentos.Select(p => p.Forma).Distinct().ToList();
            FormaSelecionada = formas.Count == 1 ? formas[0] : "misto";

            DialogResult = true;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static Color ColorFromHex(string hex) {
            hex = hex.TrimStart('#');
            return Color.FromRgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }

        private void FlashBorda() {
            // Pisca a borda do input para indicar erro
            var bd = txtValorAdicionar.Parent as Border;
            if (bd == null) return;
            var original = bd.BorderBrush;
            bd.BorderBrush = new SolidColorBrush(Colors.Red);
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            timer.Tick += (_, _) => { bd.BorderBrush = original; timer.Stop(); };
            timer.Start();
        }
    }
}
