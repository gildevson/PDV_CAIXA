using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;
using PDV_CAIXA.Services;

namespace PDV_CAIXA.Views {

    // ── Item da lista de movimentações ──────────────────────────────────────
    public class MovFechamentoItem {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
        public string   Tipo           { get; set; } = "";
        public string   Descricao      { get; set; } = "";
        public decimal  Valor          { get; set; }
        public DateTime Data           { get; set; }
        public string?  FormaPagamento { get; set; }
        public string ValorTexto => (Tipo == "entrada" ? "+ " : "− ") + Valor.ToString("C2", PtBR);
        public string DataTexto  => Data.ToString("dd/MM/yyyy  HH:mm");
        public string FormaTexto => FormaPagamento switch {
            "dinheiro" => "DINHEIRO",
            "credito"  => "CRÉDITO",
            "debito"   => "DÉBITO",
            "pix"      => "PIX",
            _          => "—"
        };
    }

    public partial class FechamentoCaixaWindow : Window {

        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        // Resultado público — lido pelo MainWindow após DialogResult = true
        public decimal SaldoRealInformado { get; private set; }
        public string? Observacao         { get; private set; }

        private readonly decimal _saldoEsperado;

        public FechamentoCaixaWindow(
            Caixa caixa,
            string nomeOperador,
            TotaisPorForma formas,
            IEnumerable<MovimentacaoCaixa> movimentacoes)
        {
            InitializeComponent();

            // ── Header ──────────────────────────────────────────────────────
            txtOperador.Text = $"Operador: {nomeOperador}";
            txtAbertura.Text = $"Aberto em {caixa.DataAbertura:dd/MM/yyyy HH:mm}";

            var dur = DateTime.Now - caixa.DataAbertura;
            txtDuracao.Text = dur.TotalHours >= 1
                ? $"Duração: {(int)dur.TotalHours}h {dur.Minutes:D2}min"
                : $"Duração: {dur.Minutes}min";

            // ── Cards por forma ──────────────────────────────────────────────
            txtFormaDinheiro.Text = formas.Dinheiro.ToString("C2", PtBR);
            txtFormaCredito.Text  = formas.Credito.ToString("C2", PtBR);
            txtFormaDebito.Text   = formas.Debito.ToString("C2", PtBR);
            txtFormaPix.Text      = formas.Pix.ToString("C2", PtBR);
            txtTotalGeral.Text    = formas.Total.ToString("C2", PtBR);
            txtSaldoInicial.Text  = $"Saldo inicial: {caixa.SaldoInicial.ToString("C2", PtBR)}";

            // ── Contagem física ──────────────────────────────────────────────
            _saldoEsperado         = caixa.SaldoInicial + formas.Dinheiro;
            txtSaldoEsperado.Text  = _saldoEsperado.ToString("C2", PtBR);
            txtDiferenca.Text      = "—";

            // ── Lista de movimentações ───────────────────────────────────────
            var itens = movimentacoes
                .OrderByDescending(m => m.Data)
                .Select(m => new MovFechamentoItem {
                    Tipo           = m.Tipo,
                    Descricao      = m.Descricao,
                    Valor          = m.Valor,
                    Data           = m.Data,
                    FormaPagamento = m.FormaPagamento
                }).ToList();

            lstMovimentacoes.ItemsSource = itens;
            txtQtdMovs.Text = itens.Count == 0
                ? "nenhuma movimentação"
                : $"{itens.Count} {(itens.Count == 1 ? "movimentação" : "movimentações")}";
        }

        // ── Atualiza diferença em tempo real ─────────────────────────────────
        private void TxtContagem_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e) {

            var texto = txtContagem.Text.Replace(",", ".").Trim();

            if (string.IsNullOrEmpty(texto)) {
                txtDiferenca.Text      = "—";
                txtDiferencaLabel.Text = "aguardando contagem...";
                txtDiferenca.Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x6A));
                btnConfirmar.IsEnabled  = false;
                return;
            }

            if (!decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var real)
                || real < 0) {
                txtDiferenca.Text      = "valor inválido";
                txtDiferencaLabel.Text = "";
                txtDiferenca.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));
                btnConfirmar.IsEnabled  = false;
                return;
            }

            var diff = real - _saldoEsperado;

            if (diff == 0) {
                txtDiferenca.Text       = "R$ 0,00";
                txtDiferencaLabel.Text  = "✓ Caixa conferido";
                txtDiferenca.Foreground = new SolidColorBrush(Color.FromRgb(0x50, 0xFA, 0x7B));
            } else if (diff > 0) {
                txtDiferenca.Text       = "+ " + diff.ToString("C2", PtBR);
                txtDiferencaLabel.Text  = "⚠ Sobra de caixa";
                txtDiferenca.Foreground = new SolidColorBrush(Color.FromRgb(0xFE, 0xBC, 0x2E));
            } else {
                txtDiferenca.Text       = "− " + Math.Abs(diff).ToString("C2", PtBR);
                txtDiferencaLabel.Text  = "⚠ Falta de caixa";
                txtDiferenca.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x55, 0x55));
            }

            btnConfirmar.IsEnabled = true;
        }

        private void TxtContagem_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"[\d,\.]");

        private void Header_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e) {
            var texto = txtContagem.Text.Replace(",", ".").Trim();
            if (!decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture,
                    out var real) || real < 0) {
                MessageBox.Show("Informe o valor contado no caixa.", "Atenção",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContagem.Focus();
                return;
            }
            SaldoRealInformado = real;
            DialogResult       = true;
        }
    }
}
