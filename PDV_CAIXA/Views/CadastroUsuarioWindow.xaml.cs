using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PDV_CAIXA.Models;
using PDV_CAIXA.Services;

namespace PDV_CAIXA.Views {
    public partial class CadastroUsuarioWindow : Window {
        private readonly UsuarioService _usuarioService = new();
        private readonly Usuario?       _usuarioEditando;
        private string                  _perfilSelecionado = "usuario";
        private byte[]?                 _fotoBytes;

        public CadastroUsuarioWindow() {
            InitializeComponent();
            SelecionarPerfil("usuario");
        }

        public CadastroUsuarioWindow(Usuario usuario) {
            InitializeComponent();
            _usuarioEditando        = usuario;
            txtTitulo.Text          = "Editar Usuário";
            txtSubtitulo.Text       = "Altere os dados do usuário";
            txtNome.Text            = usuario.Nome;
            txtSenhaHint.Visibility = Visibility.Visible;
            btnSalvar.Content       = "✔  Salvar Alterações";
            SelecionarPerfil(usuario.Perfil);

            if (usuario.Foto is { Length: > 0 }) {
                _fotoBytes = usuario.Foto;
                ExibirPreview(_fotoBytes);
                txtNomeFoto.Text          = "Foto atual carregada";
                btnRemoverFoto.Visibility = Visibility.Visible;
            }
        }

        // ── Foto ────────────────────────────────────────────────────────

        private void BtnSelecionarFoto_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Title  = "Selecionar foto de perfil",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dialog.ShowDialog() != true) return;

            var bytes = File.ReadAllBytes(dialog.FileName);

            // Redimensiona para no máximo 300x300 antes de salvar
            _fotoBytes = RedimensionarImagem(bytes, 300);

            ExibirPreview(_fotoBytes);
            txtNomeFoto.Text          = Path.GetFileName(dialog.FileName);
            btnRemoverFoto.Visibility = Visibility.Visible;
        }

        private void BtnRemoverFoto_Click(object sender, RoutedEventArgs e) {
            _fotoBytes                    = null;
            imgFotoPreview.Source         = null;
            imgFotoPreview.Visibility     = Visibility.Collapsed;
            fotoPlaceholder.Visibility    = Visibility.Visible;
            txtNomeFoto.Text              = "Nenhuma foto selecionada";
            btnRemoverFoto.Visibility     = Visibility.Collapsed;
        }

        private void ExibirPreview(byte[] bytes) {
            using var ms     = new MemoryStream(bytes);
            var bitmap       = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource    = ms;
            bitmap.CacheOption     = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            imgFotoPreview.Source      = bitmap;
            imgFotoPreview.Visibility  = Visibility.Visible;
            fotoPlaceholder.Visibility = Visibility.Collapsed;
        }

        private static byte[] RedimensionarImagem(byte[] bytes, int tamanhoMax) {
            using var ms = new MemoryStream(bytes);
            var original = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

            double escala = Math.Min((double)tamanhoMax / original.PixelWidth,
                                     (double)tamanhoMax / original.PixelHeight);

            if (escala >= 1) return bytes;

            var scaled = new TransformedBitmap(original,
                new ScaleTransform(escala, escala));

            var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
            encoder.Frames.Add(BitmapFrame.Create(scaled));

            using var saida = new MemoryStream();
            encoder.Save(saida);
            return saida.ToArray();
        }

        // ── Perfil ──────────────────────────────────────────────────────

        private void SelecionarPerfil(string perfil) {
            _perfilSelecionado = perfil;

            var corAtiva    = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var corInativa  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55"));
            var textoAtivo  = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var textoInativo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0C0"));

            cardUsuario.BorderBrush = perfil == "usuario" ? corAtiva : corInativa;
            cardAdmin.BorderBrush   = perfil == "admin"   ? corAtiva : corInativa;

            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardUsuario))
                if (tb.FontSize == 13) tb.Foreground = perfil == "usuario" ? textoAtivo : textoInativo;

            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardAdmin))
                if (tb.FontSize == 13) tb.Foreground = perfil == "admin" ? textoAtivo : textoInativo;
        }

        private void CardUsuario_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarPerfil("usuario");

        private void CardAdmin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarPerfil("admin");

        // ── Salvar ──────────────────────────────────────────────────────

        private void BtnSalvar_Click(object sender, RoutedEventArgs e) {
            LimparErros();

            var nome  = txtNome.Text.Trim();
            var senha = pwdSenha.Password;
            var valido = true;

            if (string.IsNullOrEmpty(nome)) {
                txtErroNome.Text       = "Informe o nome do usuário.";
                txtErroNome.Visibility = Visibility.Visible;
                valido = false;
            }

            if (_usuarioEditando == null && string.IsNullOrEmpty(senha)) {
                txtErroSenha.Text       = "Informe uma senha.";
                txtErroSenha.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!valido) return;

            try {
                btnSalvar.IsEnabled = false;

                if (_usuarioEditando == null) {
                    _usuarioService.Inserir(nome, senha, _perfilSelecionado, _fotoBytes);
                } else {
                    _usuarioService.Atualizar(_usuarioEditando.Id, nome, _perfilSelecionado, _fotoBytes);
                    if (!string.IsNullOrEmpty(senha))
                        _usuarioService.AlterarSenha(_usuarioEditando.Id, senha);
                }

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
            txtErroNome.Visibility  = Visibility.Collapsed;
            txtErroSenha.Visibility = Visibility.Collapsed;
            txtErroGeral.Visibility = Visibility.Collapsed;
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
