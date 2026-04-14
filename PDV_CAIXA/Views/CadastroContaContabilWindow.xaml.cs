using System.Windows;
using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Views {
    public partial class CadastroContaContabilWindow : Window {
        private readonly ContaContabilRepository _repository    = new();
        private readonly ContaContabil?          _contaEditando;

        public CadastroContaContabilWindow() {
            InitializeComponent();
            CarregarGruposEntradaSaida();
        }

        public CadastroContaContabilWindow(ContaContabil conta) {
            InitializeComponent();
            _contaEditando = conta;

            txtTitulo.Text    = "Editar Conta Contábil";
            txtSubtitulo.Text = "Altere os dados da conta";
            btnSalvar.Content = "✔  Salvar Alterações";

            txtCodigoContabil.Text  = conta.CodigoContabil;
            txtCodigoReduzido.Text  = conta.CodigoReduzido;
            txtDescricao.Text       = conta.Descricao;
            cmbGrupo.Text           = conta.Grupo ?? string.Empty;
            cmbTipo.Text            = conta.Tipo  ?? string.Empty;
            txtCodigoHistorico.Text = conta.CodigoHistorico ?? string.Empty;
            txtHistorico.Text       = conta.Historico       ?? string.Empty;
            chkExibir.IsChecked     = conta.ExibirEmLancamentosManuais;

            CarregarGruposEntradaSaida();

            cmbGrupoEntrada.Text   = conta.GrupoContabilEntrada ?? string.Empty;
            cmbGrupoSaida.Text     = conta.GrupoContabilSaida   ?? string.Empty;
            cmbCentroDeCusto.Text  = conta.CentroDeCusto        ?? string.Empty;
        }

        private void CarregarGruposEntradaSaida() {
            var grupos = _repository.ObterGrupos().ToList();
            cmbGrupoEntrada.ItemsSource  = grupos;
            cmbGrupoSaida.ItemsSource    = grupos;
            cmbCentroDeCusto.ItemsSource = grupos;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e) {
            LimparErros();
            var valido = true;

            var codigoContabil = txtCodigoContabil.Text.Trim();
            if (string.IsNullOrEmpty(codigoContabil)) {
                txtErroCodigoContabil.Text       = "Informe o código contábil.";
                txtErroCodigoContabil.Visibility = Visibility.Visible;
                valido = false;
            }

            var descricao = txtDescricao.Text.Trim();
            if (string.IsNullOrEmpty(descricao)) {
                txtErroDescricao.Text       = "Informe a descrição.";
                txtErroDescricao.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!valido) return;

            try {
                btnSalvar.IsEnabled = false;

                var conta = new ContaContabil {
                    Id                         = _contaEditando?.Id ?? 0,
                    CodigoContabil             = codigoContabil,
                    CodigoReduzido             = txtCodigoReduzido.Text.Trim(),
                    Descricao                  = descricao,
                    Grupo                      = NullSeVazio(cmbGrupo.Text),
                    Tipo                       = NullSeVazio(cmbTipo.Text),
                    CodigoHistorico            = NullSeVazio(txtCodigoHistorico.Text),
                    Historico                  = NullSeVazio(txtHistorico.Text),
                    GrupoContabilEntrada       = NullSeVazio(cmbGrupoEntrada.Text),
                    GrupoContabilSaida         = NullSeVazio(cmbGrupoSaida.Text),
                    CentroDeCusto              = NullSeVazio(cmbCentroDeCusto.Text),
                    ExibirEmLancamentosManuais = chkExibir.IsChecked == true
                };

                if (_contaEditando == null)
                    _repository.Inserir(conta);
                else
                    _repository.Atualizar(conta);

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
            txtErroCodigoContabil.Visibility = Visibility.Collapsed;
            txtErroDescricao.Visibility      = Visibility.Collapsed;
            txtErroGeral.Visibility          = Visibility.Collapsed;
        }

        private static string? NullSeVazio(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
