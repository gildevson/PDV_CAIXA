using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Views {
    public partial class CadastroRelatorioWindow : Window {
        private readonly RelatorioConfigRepository _repo = new();
        private readonly RelatorioConfig?          _editando;
        private bool                               _ativo = true;

        public CadastroRelatorioWindow() {
            InitializeComponent();
            SelecionarSituacao(true);
        }

        public CadastroRelatorioWindow(RelatorioConfig config) {
            InitializeComponent();
            _editando = config;

            txtTitulo.Text    = "Editar Relatório";
            txtSubtitulo.Text = "Altere os dados do relatório";
            btnSalvar.Content = "✔  Salvar Alterações";

            txtNome.Text        = config.Nome;
            txtNomeArquivo.Text = config.NomeArquivo;
            txtDescricao.Text   = config.Descricao ?? string.Empty;
            txtOrdem.Text       = config.Ordem.ToString();
            SelecionarSituacao(config.Ativo);

            // Seleciona o tipo no ComboBox
            foreach (ComboBoxItem item in cmbTipo.Items) {
                if (item.Tag as string == config.Tipo) {
                    cmbTipo.SelectedItem = item;
                    break;
                }
            }
        }

        // ── Situação ────────────────────────────────────────────────────

        private void SelecionarSituacao(bool ativo) {
            _ativo = ativo;
            var corAtiva   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var corInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55"));
            var txtAtivo   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var txtInativo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0C0"));

            cardAtivo.BorderBrush   = ativo ? corAtiva : corInativa;
            cardInativo.BorderBrush = ativo ? corInativa : corAtiva;

            foreach (var tb in FindVisualChildren<TextBlock>(cardAtivo))
                if (tb.FontSize == 13) tb.Foreground = ativo ? txtAtivo : txtInativo;
            foreach (var tb in FindVisualChildren<TextBlock>(cardInativo))
                if (tb.FontSize == 13) tb.Foreground = ativo ? txtInativo : txtAtivo;
        }

        private void CardAtivo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarSituacao(true);

        private void CardInativo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarSituacao(false);

        private void CmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (txtErroTipo != null)
                txtErroTipo.Visibility = Visibility.Collapsed;
        }

        // ── Salvar ──────────────────────────────────────────────────────

        private void BtnSalvar_Click(object sender, RoutedEventArgs e) {
            LimparErros();
            var valido = true;

            var nome = txtNome.Text.Trim();
            if (string.IsNullOrEmpty(nome)) {
                txtErroNome.Text       = "Informe o nome do relatório.";
                txtErroNome.Visibility = Visibility.Visible;
                valido = false;
            }

            if (cmbTipo.SelectedItem is not ComboBoxItem) {
                txtErroTipo.Text       = "Selecione o tipo do relatório.";
                txtErroTipo.Visibility = Visibility.Visible;
                valido = false;
            }

            var nomeArquivo = txtNomeArquivo.Text.Trim();
            if (string.IsNullOrEmpty(nomeArquivo)) {
                txtErroNomeArquivo.Text       = "Informe o nome do arquivo.";
                txtErroNomeArquivo.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!int.TryParse(txtOrdem.Text, out int ordem) || ordem < 0) {
                txtErroOrdem.Text       = "Informe um número de ordem válido (0 ou maior).";
                txtErroOrdem.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!valido) return;

            try {
                btnSalvar.IsEnabled = false;
                var tipoSelecionado = ((ComboBoxItem)cmbTipo.SelectedItem!).Tag as string ?? string.Empty;

                var config = new RelatorioConfig {
                    Id          = _editando?.Id ?? Guid.Empty,
                    Nome        = nome,
                    Descricao   = string.IsNullOrWhiteSpace(txtDescricao.Text) ? null : txtDescricao.Text.Trim(),
                    Tipo        = tipoSelecionado,
                    NomeArquivo = nomeArquivo,
                    Ordem       = ordem,
                    Ativo       = _ativo
                };

                if (_editando == null)
                    _repo.Inserir(config);
                else
                    _repo.Atualizar(config);

                DialogResult = true;
                Close();
            } catch (Exception ex) {
                txtErroGeral.Text       = "Erro ao salvar: " + ex.Message;
                txtErroGeral.Visibility = Visibility.Visible;
                btnSalvar.IsEnabled     = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void LimparErros() {
            txtErroNome.Visibility        = Visibility.Collapsed;
            txtErroTipo.Visibility        = Visibility.Collapsed;
            txtErroNomeArquivo.Visibility = Visibility.Collapsed;
            txtErroOrdem.Visibility       = Visibility.Collapsed;
            txtErroGeral.Visibility       = Visibility.Collapsed;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var grandchild in FindVisualChildren<T>(child))
                    yield return grandchild;
            }
        }
    }
}
