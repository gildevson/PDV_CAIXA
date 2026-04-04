using System.Globalization;
using System.Windows;
using System.Windows.Input;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Views {

    public class MovimentacaoFechamentoItem {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");
        public string Tipo      { get; set; } = "";
        public string Descricao { get; set; } = "";
        public decimal Valor    { get; set; }
        public DateTime Data    { get; set; }
        public string ValorTexto => (Tipo == "entrada" ? "+ " : "− ") + Valor.ToString("C2", PtBR);
        public string DataTexto  => Data.ToString("dd/MM/yyyy  HH:mm");
    }

    public partial class FechamentoCaixaWindow : Window {

        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        public FechamentoCaixaWindow(
            Caixa caixa,
            string nomeOperador,
            decimal totalEntradas,
            decimal totalSaidas,
            IEnumerable<MovimentacaoCaixa> movimentacoes)
        {
            InitializeComponent();

            // Header
            txtOperador.Text = $"Operador: {nomeOperador}";
            txtAbertura.Text = $"Aberto em {caixa.DataAbertura:dd/MM/yyyy HH:mm}";

            var duracao = DateTime.Now - caixa.DataAbertura;
            txtDuracao.Text = duracao.TotalHours >= 1
                ? $"Duração: {(int)duracao.TotalHours}h {duracao.Minutes:D2}min"
                : $"Duração: {duracao.Minutes}min";

            // Totais
            var saldoFinal = caixa.SaldoInicial + totalEntradas - totalSaidas;
            txtSaldoInicial.Text = caixa.SaldoInicial.ToString("C2", PtBR);
            txtEntradas.Text     = totalEntradas.ToString("C2", PtBR);
            txtSaidas.Text       = totalSaidas.ToString("C2", PtBR);
            txtSaldoFinal.Text   = saldoFinal.ToString("C2", PtBR);

            // Lista de movimentações
            var itens = movimentacoes.OrderByDescending(m => m.Data)
                .Select(m => new MovimentacaoFechamentoItem {
                    Tipo      = m.Tipo,
                    Descricao = m.Descricao,
                    Valor     = m.Valor,
                    Data      = m.Data
                }).ToList();

            lstMovimentacoes.ItemsSource = itens;
            txtQtdMovs.Text = itens.Count == 0
                ? "nenhuma movimentação"
                : $"{itens.Count} {(itens.Count == 1 ? "movimentação" : "movimentações")}";
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;
    }
}
