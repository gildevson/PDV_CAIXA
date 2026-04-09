using System.Windows;
using System.Windows.Controls;
using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Views {
    public partial class GerenciarRelatoriosWindow : Window {
        private readonly RelatorioConfigRepository _repo = new();

        public GerenciarRelatoriosWindow() {
            InitializeComponent();
            Carregar();
        }

        private void Carregar() {
            dgConfigs.ItemsSource = _repo.ObterTodos().ToList();
        }

        private void BtnNovo_Click(object sender, RoutedEventArgs e) {
            var win = new CadastroRelatorioWindow { Owner = this };
            if (win.ShowDialog() == true)
                Carregar();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button btn || btn.Tag is not RelatorioConfig config) return;
            var win = new CadastroRelatorioWindow(config) { Owner = this };
            if (win.ShowDialog() == true)
                Carregar();
        }

        private void BtnExcluir_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button btn || btn.Tag is not RelatorioConfig config) return;
            var resp = MessageBox.Show($"Excluir o relatório:\n{config.Nome}?",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp != MessageBoxResult.Yes) return;
            try {
                _repo.Excluir(config.Id);
                Carregar();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao excluir:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();
    }
}
