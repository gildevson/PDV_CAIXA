using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PDV_CAIXA.Models;
using PDV_CAIXA.Services;

namespace PDV_CAIXA.Views {
    public partial class CadastroProdutoWindow : Window {
        private readonly ProdutoService _produtoService = new();
        private readonly Produto?       _produtoEditando;
        private bool                    _ativo = true;
        private bool                    _vendidoPorPeso = false;
        private byte[]?                 _fotoBytes;

        public CadastroProdutoWindow() {
            InitializeComponent();
            SelecionarSituacao(true);
            SelecionarTipoVenda(false);
        }

        public CadastroProdutoWindow(Produto produto) {
            InitializeComponent();
            _produtoEditando      = produto;
            txtTitulo.Text        = "Editar Produto";
            txtSubtitulo.Text     = "Altere os dados do produto";
            btnSalvar.Content     = "✔  Salvar Alterações";

            txtNome.Text          = produto.Nome;
            txtCodigoBarras.Text  = produto.CodigoBarras ?? string.Empty;
            txtPreco.Text         = produto.Preco.ToString("N2", new CultureInfo("pt-BR"));
            txtDesconto.Text      = produto.Desconto.ToString("F0", CultureInfo.InvariantCulture);
            txtEstoque.Text       = produto.Estoque.ToString();
            txtDescricao.Text     = produto.Descricao ?? string.Empty;
            SelecionarSituacao(produto.Ativo);
            SelecionarTipoVenda(produto.VendidoPorPeso);
            AtualizarPreviewDesconto();

            if (produto.Foto is { Length: > 0 }) {
                _fotoBytes = produto.Foto;
                ExibirPreview(_fotoBytes);
                txtNomeFoto.Text          = "Foto atual carregada";
                btnRemoverFoto.Visibility = Visibility.Visible;
            }
        }

        // ── Foto ────────────────────────────────────────────────────────

        private void BtnSelecionarFoto_Click(object sender, RoutedEventArgs e) {
            var dialog = new OpenFileDialog {
                Title  = "Selecionar foto do produto",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (dialog.ShowDialog() != true) return;

            _fotoBytes = RedimensionarImagem(File.ReadAllBytes(dialog.FileName), 400);
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
            using var ms = new MemoryStream(bytes);
            var bitmap   = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = ms;
            bitmap.CacheOption  = BitmapCacheOption.OnLoad;
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
            var scaled  = new TransformedBitmap(original, new ScaleTransform(escala, escala));
            var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            using var saida = new MemoryStream();
            encoder.Save(saida);
            return saida.ToArray();
        }

        // ── Desconto preview ────────────────────────────────────────────

        private void TxtDesconto_TextChanged(object sender, TextChangedEventArgs e)
            => AtualizarPreviewDesconto();

        private void AtualizarPreviewDesconto() {
            if (txtDesconto == null || txtPreco == null ||
                borderPreviewDesconto == null || txtPreviewDesconto == null) return;

            decimal.TryParse(txtPreco.Text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var preco);
            decimal.TryParse(txtDesconto.Text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var desc);

            if (desc > 0 && desc <= 100 && preco > 0) {
                var precoFinal = preco * (1 - desc / 100m);
                txtPreviewDesconto.Text          = $"Preço com desconto: {precoFinal.ToString("C2", new CultureInfo("pt-BR"))}  (era {preco.ToString("C2", new CultureInfo("pt-BR"))})";
                borderPreviewDesconto.Visibility = Visibility.Visible;
            } else {
                borderPreviewDesconto.Visibility = Visibility.Collapsed;
            }
        }

        // ── Tipo de venda ───────────────────────────────────────────────

        private void SelecionarTipoVenda(bool porPeso) {
            _vendidoPorPeso = porPeso;
            var corAtiva   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var corInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55"));
            var txtAtivo   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var txtInativo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0C0"));

            cardPorUnidade.BorderBrush = porPeso ? corInativa : corAtiva;
            cardPorPeso.BorderBrush    = porPeso ? corAtiva : corInativa;

            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardPorUnidade))
                if (tb.FontSize == 13) tb.Foreground = porPeso ? txtInativo : txtAtivo;
            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardPorPeso))
                if (tb.FontSize == 13) tb.Foreground = porPeso ? txtAtivo : txtInativo;

            if (lblPreco != null)
                lblPreco.Text = porPeso ? "PREÇO (R$/kg)" : "PREÇO (R$)";
        }

        private void CardPorUnidade_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarTipoVenda(false);

        private void CardPorPeso_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarTipoVenda(true);

        // ── Situação ────────────────────────────────────────────────────

        private void SelecionarSituacao(bool ativo) {
            _ativo = ativo;
            var corAtiva   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var corInativa = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55"));
            var txtAtivo   = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C83FF"));
            var txtInativo = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0A0C0"));

            cardAtivo.BorderBrush   = ativo ? corAtiva : corInativa;
            cardInativo.BorderBrush = ativo ? corInativa : corAtiva;

            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardAtivo))
                if (tb.FontSize == 13) tb.Foreground = ativo ? txtAtivo : txtInativo;
            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBlock>(cardInativo))
                if (tb.FontSize == 13) tb.Foreground = ativo ? txtInativo : txtAtivo;
        }

        private void CardAtivo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarSituacao(true);

        private void CardInativo_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => SelecionarSituacao(false);

        // ── Salvar ──────────────────────────────────────────────────────

        private void BtnSalvar_Click(object sender, RoutedEventArgs e) {
            LimparErros();
            var valido = true;

            var nome = txtNome.Text.Trim();
            if (string.IsNullOrEmpty(nome)) {
                txtErroNome.Text       = "Informe o nome do produto.";
                txtErroNome.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!decimal.TryParse(txtPreco.Text.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal preco) || preco < 0) {
                txtErroPreco.Text       = "Preço inválido.";
                txtErroPreco.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!decimal.TryParse(txtDesconto.Text.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal desconto)
                    || desconto < 0 || desconto > 100) {
                txtErroDesconto.Text       = "Desconto deve ser 0 a 100.";
                txtErroDesconto.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!int.TryParse(txtEstoque.Text, out int estoque) || estoque < 0) {
                txtErroEstoque.Text       = "Estoque inválido.";
                txtErroEstoque.Visibility = Visibility.Visible;
                valido = false;
            }

            if (!valido) return;

            try {
                btnSalvar.IsEnabled = false;

                var produto = new Produto {
                    Id             = _produtoEditando?.Id ?? Guid.Empty,
                    Nome           = nome,
                    CodigoBarras   = string.IsNullOrWhiteSpace(txtCodigoBarras.Text) ? null : txtCodigoBarras.Text.Trim(),
                    Preco          = preco,
                    Desconto       = desconto,
                    Estoque        = estoque,
                    Descricao      = string.IsNullOrWhiteSpace(txtDescricao.Text) ? null : txtDescricao.Text.Trim(),
                    Ativo          = _ativo,
                    VendidoPorPeso = _vendidoPorPeso,
                    Foto           = _fotoBytes
                };

                if (_produtoEditando == null)
                    _produtoService.Inserir(produto);
                else
                    _produtoService.Atualizar(produto);

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
            txtErroNome.Visibility     = Visibility.Collapsed;
            txtErroPreco.Visibility    = Visibility.Collapsed;
            txtErroDesconto.Visibility = Visibility.Collapsed;
            txtErroEstoque.Visibility  = Visibility.Collapsed;
            txtErroGeral.Visibility    = Visibility.Collapsed;
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
