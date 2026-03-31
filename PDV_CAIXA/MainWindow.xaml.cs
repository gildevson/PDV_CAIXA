using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using PDV_CAIXA.Models;
using PDV_CAIXA.Services;
using PDV_CAIXA.ViewModels;
using PDV_CAIXA.Views;

namespace PDV_CAIXA {
    public partial class MainWindow : Window {
        private readonly Usuario        _usuarioLogado;
        private readonly UsuarioService _usuarioService = new();

        public MainWindow(Usuario usuario) {
            InitializeComponent();
            _usuarioLogado = usuario;
            ConfigurarPerfil();
        }

        private void ConfigurarPerfil() {
            txtNomeUsuario.Text  = _usuarioLogado.Nome;
            txtPerfilUsuario.Text = _usuarioLogado.Perfil == "admin" ? "Administrador" : "Usuário";
            txtBemVindo.Text      = $"Logado como {_usuarioLogado.Nome} ({txtPerfilUsuario.Text})";

            // Iniciais como fallback caso não tenha foto
            txtIniciais.Text = ObterIniciais(_usuarioLogado.Nome);

            // Carrega foto do usuário logado (busca completa com foto)
            var completo = _usuarioService.ObterPorId(_usuarioLogado.Id);
            if (completo?.Foto is { Length: > 0 })
                CarregarFotoAvatar(completo.Foto);

            if (_usuarioLogado.Perfil == "admin")
                btnMenuUsuarios.Visibility = Visibility.Visible;
        }

        private void CarregarFotoAvatar(byte[] fotoBytes) {
            using var ms = new MemoryStream(fotoBytes);
            var bitmap  = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption  = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            imgAvatarBrush.ImageSource  = bitmap;
            ellipseAvatar.Visibility    = Visibility.Visible;
            txtIniciais.Visibility      = Visibility.Collapsed;
        }

        private static string ObterIniciais(string nome) {
            var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return partes.Length >= 2
                ? $"{partes[0][0]}{partes[^1][0]}".ToUpper()
                : nome.Length >= 2 ? nome[..2].ToUpper() : nome.ToUpper();
        }

        // ── Navegação ────────────────────────────────────────────────

        private void MostrarPagina(UIElement pagina) {
            pageInicio.Visibility   = Visibility.Collapsed;
            pageUsuarios.Visibility = Visibility.Collapsed;
            pagina.Visibility       = Visibility.Visible;
        }

        private void BtnMenuInicio_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageInicio);
            btnMenuInicio.Style   = (Style)FindResource("MenuButtonActive");
            btnMenuUsuarios.Style = (Style)FindResource("MenuButton");
        }

        private void BtnMenuUsuarios_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageUsuarios);
            btnMenuInicio.Style   = (Style)FindResource("MenuButton");
            btnMenuUsuarios.Style = (Style)FindResource("MenuButtonActive");
            CarregarUsuarios();
        }

        // ── Usuários ─────────────────────────────────────────────────

        private void CarregarUsuarios() {
            dgUsuarios.ItemsSource = _usuarioService.ObterTodos()
                .Select(u => new UsuarioViewModel {
                    Id            = u.Id,
                    Nome          = u.Nome,
                    Perfil        = u.Perfil,
                    Foto          = u.Foto,
                    IsCurrentUser = u.Id == _usuarioLogado.Id
                }).ToList();
        }

        private void BtnNovoUsuario_Click(object sender, RoutedEventArgs e) {
            var janela = new CadastroUsuarioWindow();
            if (janela.ShowDialog() == true)
                CarregarUsuarios();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is UsuarioViewModel vm) {
                var usuarioCompleto = _usuarioService.ObterPorId(vm.Id);
                if (usuarioCompleto == null) return;

                var janela = new CadastroUsuarioWindow(usuarioCompleto);
                if (janela.ShowDialog() == true) {
                    if (vm.Id == _usuarioLogado.Id) {
                        var atualizado = _usuarioService.ObterPorId(_usuarioLogado.Id);
                        if (atualizado?.Foto is { Length: > 0 })
                            CarregarFotoAvatar(atualizado.Foto);
                        else {
                            ellipseAvatar.Visibility = Visibility.Collapsed;
                            txtIniciais.Visibility   = Visibility.Visible;
                        }
                    }
                    CarregarUsuarios();
                }
            }
        }

        private void BtnExcluir_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is UsuarioViewModel vm) {
                var confirm = MessageBox.Show(
                    $"Excluir o usuário \"{vm.Nome}\"?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirm == MessageBoxResult.Yes) {
                    _usuarioService.Excluir(vm.Id);
                    CarregarUsuarios();
                }
            }
        }

        private void BtnSair_Click(object sender, RoutedEventArgs e) {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
