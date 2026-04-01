using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PDV_CAIXA.Models;
using PDV_CAIXA.Services;
using PDV_CAIXA.ViewModels;
using PDV_CAIXA.Views;

namespace PDV_CAIXA {
    public partial class MainWindow : Window {
        private readonly Usuario        _usuarioLogado;
        private readonly UsuarioService _usuarioService = new();
        private readonly ProdutoService _produtoService = new();
        private readonly PedidoService  _pedidoService  = new();
        private List<ProdutoViewModel>  _todosProdutos  = new();
        private bool                    _filtroSoAtivos = false;

        // ── PDV ──────────────────────────────────────────────────────
        private readonly ObservableCollection<CarrinhoItemViewModel> _carrinho = new();
        private List<ProdutoViewModel> _pdvTodosProdutos = new();
        private bool _pdvInicializado = false;

        public MainWindow(Usuario usuario) {
            InitializeComponent();
            _usuarioLogado = usuario;
            ConfigurarPerfil();
        }

        private void ConfigurarPerfil() {
            txtNomeUsuario.Text   = _usuarioLogado.Nome;
            txtPerfilUsuario.Text = _usuarioLogado.Perfil == "admin" ? "Administrador" : "Usuário";
            txtBemVindo.Text      = $"Logado como {_usuarioLogado.Nome} ({txtPerfilUsuario.Text})";

            txtIniciais.Text = ObterIniciais(_usuarioLogado.Nome);

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

            imgAvatarBrush.ImageSource = bitmap;
            ellipseAvatar.Visibility   = Visibility.Visible;
            txtIniciais.Visibility     = Visibility.Collapsed;
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
            pagePDV.Visibility      = Visibility.Collapsed;
            pageProdutos.Visibility = Visibility.Collapsed;
            pageUsuarios.Visibility = Visibility.Collapsed;
            pagina.Visibility       = Visibility.Visible;
        }

        private void ResetarMenus() {
            btnMenuInicio.Style   = (Style)FindResource("MenuButton");
            btnMenuPDV.Style      = (Style)FindResource("MenuButton");
            btnMenuProdutos.Style = (Style)FindResource("MenuButton");
            btnMenuUsuarios.Style = (Style)FindResource("MenuButton");
        }

        private void BtnMenuInicio_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageInicio);
            ResetarMenus();
            btnMenuInicio.Style = (Style)FindResource("MenuButtonActive");
        }

        private void BtnMenuPDV_Click(object sender, RoutedEventArgs e) {
            try {
                MostrarPagina(pagePDV);
                ResetarMenus();
                btnMenuPDV.Style = (Style)FindResource("MenuButtonActive");
                InicializarPDV();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao abrir o PDV:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnMenuProdutos_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageProdutos);
            ResetarMenus();
            btnMenuProdutos.Style = (Style)FindResource("MenuButtonActive");
            CarregarProdutos();
        }

        private void BtnMenuUsuarios_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageUsuarios);
            ResetarMenus();
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

        // ── Produtos ─────────────────────────────────────────────────

        private void CarregarProdutos() {
            _todosProdutos = _produtoService.ObterTodos()
                .Select(p => new ProdutoViewModel {
                    Id           = p.Id,
                    Nome         = p.Nome,
                    Descricao    = p.Descricao,
                    CodigoBarras = p.CodigoBarras,
                    Preco        = p.Preco,
                    Estoque      = p.Estoque,
                    Ativo        = p.Ativo,
                    Foto         = p.Foto
                }).ToList();

            AplicarFiltroProdutos();
        }

        private void AplicarFiltroProdutos() {
            var busca = txtBuscaProduto.Text.Trim().ToLower();

            var filtrado = _todosProdutos
                .Where(p => !_filtroSoAtivos || p.Ativo)
                .Where(p => string.IsNullOrEmpty(busca)
                         || p.Nome.ToLower().Contains(busca)
                         || (p.CodigoBarras?.ToLower().Contains(busca) ?? false))
                .ToList();

            dgProdutos.ItemsSource   = filtrado;
            txtContadorProdutos.Text = $"{filtrado.Count} produto(s) encontrado(s)";
            txtFiltroAtivo.Text      = _filtroSoAtivos ? "Somente ativos ✔" : "Todos";
        }

        private void TxtBuscaProduto_TextChanged(object sender, TextChangedEventArgs e)
            => AplicarFiltroProdutos();

        private void FiltroAtivo_Click(object sender, MouseButtonEventArgs e) {
            _filtroSoAtivos = !_filtroSoAtivos;
            AplicarFiltroProdutos();
        }

        private void BtnNovoProduto_Click(object sender, RoutedEventArgs e) {
            var janela = new CadastroProdutoWindow();
            if (janela.ShowDialog() == true)
                CarregarProdutos();
        }

        private void BtnEditarProduto_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is ProdutoViewModel vm) {
                var produto = _produtoService.ObterPorId(vm.Id);
                if (produto == null) return;
                var janela = new CadastroProdutoWindow(produto);
                if (janela.ShowDialog() == true)
                    CarregarProdutos();
            }
        }

        private void BtnExcluirProduto_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is ProdutoViewModel vm) {
                var confirm = MessageBox.Show(
                    $"Excluir o produto \"{vm.Nome}\"?", "Confirmar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes) {
                    _produtoService.Excluir(vm.Id);
                    CarregarProdutos();
                }
            }
        }

        // ── PDV ──────────────────────────────────────────────────────

        private void InicializarPDV() {
            if (!_pdvInicializado) {
                pdvDgCarrinho.ItemsSource = _carrinho;
                _pdvInicializado = true;
            }
            PdvCarregarProdutos();
            PdvAtualizarVisibilidade();
        }

        private void PdvCarregarProdutos() {
            try {
                _pdvTodosProdutos = _produtoService.ObterTodos()
                    .Where(p => p.Ativo && p.Estoque > 0)
                    .Select(p => new ProdutoViewModel {
                        Id           = p.Id,
                        Nome         = p.Nome,
                        CodigoBarras = p.CodigoBarras,
                        Preco        = p.Preco,
                        Estoque      = p.Estoque,
                        Ativo        = p.Ativo
                    }).ToList();
                PdvAplicarFiltro();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar produtos:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PdvAplicarFiltro() {
            var busca = pdvTxtBusca.Text.Trim().ToLower();
            var resultado = string.IsNullOrEmpty(busca)
                ? _pdvTodosProdutos
                : _pdvTodosProdutos
                    .Where(p => p.Nome.ToLower().Contains(busca)
                             || (p.CodigoBarras?.ToLower().Contains(busca) ?? false))
                    .ToList();

            pdvLstProdutos.ItemsSource = resultado;

            // Se encontrou exatamente 1, seleciona automaticamente
            if (resultado.Count == 1) {
                pdvLstProdutos.SelectedIndex = 0;
                pdvBtnAdicionar.IsEnabled = true;
            } else {
                pdvBtnAdicionar.IsEnabled = pdvLstProdutos.SelectedItem != null;
            }
        }

        private void PdvTxtBusca_TextChanged(object sender, TextChangedEventArgs e)
            => PdvAplicarFiltro();

        private void PdvTxtBusca_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Down && pdvLstProdutos.Items.Count > 0) {
                pdvLstProdutos.SelectedIndex = 0;
                pdvLstProdutos.Focus();
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                PdvAdicionarAoCarrinho();
                e.Handled = true;
            }
        }

        private void PdvLstProdutos_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            pdvBtnAdicionar.IsEnabled = pdvLstProdutos.SelectedItem != null;
        }

        private void PdvLstProdutos_DoubleClick(object sender, MouseButtonEventArgs e) {
            PdvAdicionarAoCarrinho();
        }

        private void PdvBtnAdicionar_Click(object sender, RoutedEventArgs e)
            => PdvAdicionarAoCarrinho();

        private void PdvAdicionarAoCarrinho() {
            if (pdvLstProdutos.SelectedItem is not ProdutoViewModel produto) {
                if (pdvLstProdutos.Items.Count == 1)
                    pdvLstProdutos.SelectedIndex = 0;
                else {
                    pdvTxtBusca.Focus();
                    return;
                }
                if (pdvLstProdutos.SelectedItem is not ProdutoViewModel p2) return;
                produto = p2;
            }

            if (!int.TryParse(pdvTxtQtd.Text, out var qtd) || qtd < 1)
                qtd = 1;

            var existente = _carrinho.FirstOrDefault(c => c.ProdutoId == produto.Id);
            if (existente != null) {
                existente.Quantidade += qtd;
            } else {
                _carrinho.Add(new CarrinhoItemViewModel {
                    ProdutoId     = produto.Id,
                    Nome          = produto.Nome,
                    PrecoUnitario = produto.Preco,
                    Quantidade    = qtd
                });
            }

            pdvTxtQtd.Text           = "1";
            pdvLstProdutos.SelectedItem = null;
            pdvTxtBusca.Text         = string.Empty;
            pdvTxtBusca.Focus();

            PdvAtualizarTotal();
            PdvAtualizarVisibilidade();
        }

        private void PdvBtnMais_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is CarrinhoItemViewModel item) {
                item.Quantidade++;
                PdvAtualizarTotal();
            }
        }

        private void PdvBtnMenos_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is CarrinhoItemViewModel item) {
                if (item.Quantidade > 1)
                    item.Quantidade--;
                else
                    _carrinho.Remove(item);
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
            }
        }

        private void PdvBtnRemoverItem_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is CarrinhoItemViewModel item) {
                _carrinho.Remove(item);
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
            }
        }

        private void PdvBtnLimpar_Click(object sender, RoutedEventArgs e) {
            if (_carrinho.Count == 0) return;
            var confirm = MessageBox.Show(
                "Limpar o carrinho?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes) {
                _carrinho.Clear();
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
            }
        }

        private void PdvBtnFinalizar_Click(object sender, RoutedEventArgs e) {
            if (_carrinho.Count == 0) return;

            var total = _carrinho.Sum(i => i.Subtotal).ToString("C2",
                new System.Globalization.CultureInfo("pt-BR"));

            var confirm = MessageBox.Show(
                $"Finalizar pedido?\n\nTotal: {total}",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try {
                var itens = _carrinho.Select(c => new PedidoItem {
                    ProdutoId      = c.ProdutoId,
                    NomeProduto    = c.Nome,
                    Quantidade     = c.Quantidade,
                    PrecoUnitario  = c.PrecoUnitario,
                    Subtotal       = c.Subtotal
                }).ToList();

                _pedidoService.Finalizar(_usuarioLogado.Id, itens);

                _carrinho.Clear();
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
                PdvCarregarProdutos(); // recarrega estoque atualizado

                MessageBox.Show("Pedido finalizado com sucesso!", "Sucesso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao finalizar pedido:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PdvAtualizarTotal() {
            var total = _carrinho.Sum(i => i.Subtotal);
            pdvTxtTotal.Text      = total.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
            pdvTxtTotalItens.Text = $"{_carrinho.Sum(i => i.Quantidade)} item(ns)";
        }

        private void PdvAtualizarVisibilidade() {
            var temItens = _carrinho.Count > 0;
            pdvDgCarrinho.Visibility  = temItens ? Visibility.Visible   : Visibility.Collapsed;
            pdvPanelVazio.Visibility  = temItens ? Visibility.Collapsed : Visibility.Visible;
            pdvBtnFinalizar.IsEnabled = temItens;
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (pagePDV.Visibility != Visibility.Visible) return;

            if (e.Key == Key.F12) {
                PdvBtnFinalizar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            } else if (e.Key == Key.Delete) {
                PdvBtnLimpar_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ── Sair ─────────────────────────────────────────────────────

        private void BtnSair_Click(object sender, RoutedEventArgs e) {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
