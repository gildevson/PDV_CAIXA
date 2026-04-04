using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PDV_CAIXA.Services;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Views {

    public class MovimentacaoHistoricoItem {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
        public string  Tipo      { get; set; } = "";
        public string  Descricao { get; set; } = "";
        public decimal Valor     { get; set; }
        public DateTime Data     { get; set; }
        public string  Origem    { get; set; } = "";
        public string ValorTexto  => (Tipo == "entrada" ? "+ " : "− ") + Valor.ToString("C2", PtBR);
        public string DataTexto   => Data.ToString("dd/MM/yyyy  HH:mm");
        public string OrigemTexto => Origem == "venda" ? "venda" : "manual";
    }

    public partial class HistoricoCaixaWindow : Window {

        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        private readonly CaixaService _service = new();
        private List<CaixaSessaoViewModel> _todasSessoes = new();
        private CaixaSessaoViewModel? _sessaoSelecionada;

        public HistoricoCaixaWindow() {
            InitializeComponent();
            Loaded += (_, _) => Carregar();
        }

        private void Carregar() {
            try {
                _todasSessoes = _service.ListarHistorico().ToList();
                AplicarFiltro();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar histórico:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltro() {
            var filtro = txtFiltro.Text.Trim().ToLowerInvariant();
            var lista  = string.IsNullOrEmpty(filtro)
                ? _todasSessoes
                : _todasSessoes.Where(s =>
                    s.AberturaTexto.Contains(filtro, StringComparison.OrdinalIgnoreCase)   ||
                    s.FechamentoTexto.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                    s.NomeOperador.Contains(filtro, StringComparison.OrdinalIgnoreCase)    ||
                    s.Status.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                .ToList();

            lstSessoes.ItemsSource = lista;
            txtTotalSessoes.Text   = $"{lista.Count} {(lista.Count == 1 ? "sessão" : "sessões")} encontrada(s)";
        }

        private void ExibirDetalhe(CaixaSessaoViewModel sessao) {
            _sessaoSelecionada = sessao;

            // Header
            if (sessao.Status == "aberto") {
                bdStatusDetalhe.Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x1A, 0x0E));
                txtStatusDetalhe.Text       = "● ABERTO";
                txtStatusDetalhe.Foreground = new SolidColorBrush(Color.FromRgb(0x50, 0xFA, 0x7B));
            } else {
                bdStatusDetalhe.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x08, 0x08));
                txtStatusDetalhe.Text       = "⏹ FECHADO";
                txtStatusDetalhe.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x33, 0x33));
            }

            txtDetalheAbertura.Text   = $"Abertura: {sessao.AberturaTexto}";
            txtDetalheOperador.Text   = $"Operador: {sessao.NomeOperador}";
            txtDetalheDuracao.Text    = sessao.DuracaoTexto;
            txtDetalheFechamento.Text = $"Fechamento: {sessao.FechamentoTexto}";

            // Cards
            txtCardSaldoInicial.Text = sessao.SaldoInicialTexto;
            txtCardEntradas.Text     = sessao.EntradasTexto;
            txtCardSaidas.Text       = sessao.SaidasTexto;
            txtCardSaldoFinal.Text   = sessao.SaldoFinalTexto;

            // Movimentações
            try {
                var movs = _service.ListarMovimentacoes(sessao.Id)
                    .OrderByDescending(m => m.Data)
                    .Select(m => new MovimentacaoHistoricoItem {
                        Tipo      = m.Tipo,
                        Descricao = m.Descricao,
                        Valor     = m.Valor,
                        Data      = m.Data,
                        Origem    = m.Origem
                    }).ToList();

                lstMovs.ItemsSource = movs;
                txtQtdMovs.Text     = movs.Count == 0
                    ? "nenhuma movimentação"
                    : $"{movs.Count} {(movs.Count == 1 ? "movimentação" : "movimentações")}";
            } catch {
                lstMovs.ItemsSource = new List<MovimentacaoHistoricoItem>();
                txtQtdMovs.Text = "erro ao carregar";
            }

            panelVazio.Visibility   = Visibility.Collapsed;
            panelDetalhe.Visibility = Visibility.Visible;
        }

        private void TxtFiltro_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => AplicarFiltro();

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement fe && fe.DataContext is CaixaSessaoViewModel sessao)
                ExibirDetalhe(sessao);
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
