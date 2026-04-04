using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;
using PDV_CAIXA.Services;
using PDV_CAIXA.ViewModels;
using PDV_CAIXA.Views;

namespace PDV_CAIXA {
    public partial class MainWindow : Window {
        private readonly Usuario           _usuarioLogado;
        private readonly UsuarioService    _usuarioService    = new();
        private readonly ProdutoService    _produtoService    = new();
        private readonly PedidoService     _pedidoService     = new();
        private readonly PedidoRepository  _pedidoRepository  = new();
        private readonly CaixaService      _caixaService      = new();
        private List<ProdutoViewModel>     _todosProdutos     = new();
        private bool                       _filtroSoAtivos    = false;

        // ── Caixa ────────────────────────────────────────────────────
        private Models.Caixa? _caixaAtivo;

        // ── PDV ──────────────────────────────────────────────────────
        private readonly ObservableCollection<CarrinhoItemViewModel> _carrinho = new();
        private List<ProdutoViewModel> _pdvTodosProdutos = new();
        private bool _pdvInicializado = false;

        // ── Histórico ────────────────────────────────────────────────

        private void HistCarregarFiltro(DateTime? inicio) {
            try {
                var lista = _pedidoRepository.ListarPorPeriodo(inicio).ToList();
                var (qtd, fat) = _pedidoRepository.ObterTotais(inicio);

                histDgPedidos.ItemsSource  = lista;
                histDgItens.ItemsSource    = null;
                histTxtDetalhe.Text        = "Selecione um pedido para ver os itens";
                histTxtQtdPedidos.Text     = qtd.ToString();
                histTxtFaturamento.Text    = fat.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
                histTxtContador.Text       = $"{lista.Count} pedido(s)";
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar histórico:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HistFiltro_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            foreach (var b in new[] { histFiltroHoje, histFiltroSemana, histFiltroMes, histFiltroTodos }) {
                b.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E));
                ((System.Windows.Controls.TextBlock)b.Child).Foreground =
                    new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0xA0, 0xA0, 0xC0));
            }
            var sel = (Border)sender;
            sel.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x3E));
            ((System.Windows.Controls.TextBlock)sel.Child).Foreground =
                new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x7C, 0x83, 0xFF));

            DateTime? inicio = sel.Name switch {
                "histFiltroHoje"   => DateTime.Today,
                "histFiltroSemana" => DateTime.Today.AddDays(-6),
                "histFiltroMes"    => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                _                  => null
            };
            HistCarregarFiltro(inicio);
        }

        private void HistDgPedidos_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (histDgPedidos.SelectedItem is not PedidoResumoViewModel pedido) return;
            try {
                histDgItens.ItemsSource = _pedidoRepository.ObterItens(pedido.Id).ToList();
                histTxtDetalhe.Text     = $"Itens do Pedido {pedido.NumeroTexto}  —  {pedido.TotalTexto}";
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar itens:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Pedidos (pesquisa) ────────────────────────────────────────
        private string _pedidoNumeroBuffer = string.Empty;

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
            pageInicio.Visibility    = Visibility.Collapsed;
            pagePDV.Visibility       = Visibility.Collapsed;
            pageProdutos.Visibility  = Visibility.Collapsed;
            pageUsuarios.Visibility  = Visibility.Collapsed;
            pagePedidos.Visibility   = Visibility.Collapsed;
            pageHistorico.Visibility = Visibility.Collapsed;
            pageCaixa.Visibility     = Visibility.Collapsed;
            pagina.Visibility        = Visibility.Visible;
        }

        private void ResetarMenus() {
            btnMenuInicio.Style    = (Style)FindResource("MenuButton");
            btnMenuPDV.Style       = (Style)FindResource("MenuButton");
            btnMenuProdutos.Style  = (Style)FindResource("MenuButton");
            btnMenuUsuarios.Style  = (Style)FindResource("MenuButton");
            btnMenuPedidos.Style   = (Style)FindResource("MenuButton");
            btnMenuHistorico.Style = (Style)FindResource("MenuButton");
            btnMenuCaixa.Style     = (Style)FindResource("MenuButton");
        }

        // ── Overlay de carregamento ──────────────────────────────────

        private void MostrarCarregando(string mensagem = "Carregando...") {
            overlayTxtMensagem.Text      = mensagem;
            overlayCarregando.Visibility = Visibility.Visible;
        }

        private void EsconderCarregando() {
            overlayCarregando.Visibility = Visibility.Collapsed;
        }

        // ── Navegação (async) ────────────────────────────────────────

        private void BtnMenuInicio_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageInicio);
            ResetarMenus();
            btnMenuInicio.Style = (Style)FindResource("MenuButtonActive");
        }

        private async void BtnMenuPDV_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pagePDV);
            ResetarMenus();
            btnMenuPDV.Style = (Style)FindResource("MenuButtonActive");
            MostrarCarregando("Abrindo PDV...");
            try {
                if (!_pdvInicializado) {
                    pdvDgCarrinho.ItemsSource = _carrinho;
                    _pdvInicializado = true;
                }
                var produtos = await Task.Run(() =>
                    _produtoService.ObterTodos()
                        .Where(p => p.Ativo && p.Estoque > 0)
                        .Select(p => new ProdutoViewModel {
                            Id = p.Id, Nome = p.Nome, CodigoBarras = p.CodigoBarras,
                            Preco = p.Preco, Desconto = p.Desconto, Estoque = p.Estoque, Ativo = p.Ativo
                        }).ToList());
                _pdvTodosProdutos = produtos;
                PdvAplicarFiltro();
                PdvAtualizarVisibilidade();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao abrir o PDV:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                EsconderCarregando();
            }
        }

        private async void BtnMenuProdutos_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageProdutos);
            ResetarMenus();
            btnMenuProdutos.Style = (Style)FindResource("MenuButtonActive");
            MostrarCarregando("Carregando produtos...");
            try {
                var lista = await Task.Run(() => _produtoService.ObterTodos().ToList());
                _todosProdutos = lista.Select(p => new ProdutoViewModel {
                    Id = p.Id, Nome = p.Nome, Descricao = p.Descricao,
                    CodigoBarras = p.CodigoBarras, Preco = p.Preco, Desconto = p.Desconto,
                    Estoque = p.Estoque, Ativo = p.Ativo, Foto = p.Foto
                }).ToList();
                AplicarFiltroProdutos();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar produtos:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                EsconderCarregando();
            }
        }

        private async void BtnMenuHistorico_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageHistorico);
            ResetarMenus();
            btnMenuHistorico.Style = (Style)FindResource("MenuButtonActive");
            MostrarCarregando("Carregando histórico...");
            try {
                var inicio = DateTime.Today;
                var (lista, qtd, fat) = await Task.Run(() => {
                    var l = _pedidoRepository.ListarPorPeriodo(inicio).ToList();
                    var (q, f) = _pedidoRepository.ObterTotais(inicio);
                    return (l, q, f);
                });
                histDgPedidos.ItemsSource = lista;
                histDgItens.ItemsSource   = null;
                histTxtDetalhe.Text       = "Selecione um pedido para ver os itens";
                histTxtQtdPedidos.Text    = qtd.ToString();
                histTxtFaturamento.Text   = fat.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
                histTxtContador.Text      = $"{lista.Count} pedido(s)";
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar histórico:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                EsconderCarregando();
            }
        }

        private async void BtnMenuPedidos_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pagePedidos);
            ResetarMenus();
            btnMenuPedidos.Style = (Style)FindResource("MenuButtonActive");
            MostrarCarregando("Carregando pedidos...");
            try {
                var lista = await Task.Run(() => _pedidoRepository.ListarRecentes(30).ToList());
                dgPedidos.ItemsSource     = lista;
                dgItensPedido.ItemsSource = null;
                pedidosTxtContador.Text   = $"{lista.Count} pedido(s) recente(s)";
                pedidosTxtDetalhe.Text    = "Selecione um pedido para ver os itens";
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar pedidos:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                EsconderCarregando();
            }
        }

        private async void BtnMenuUsuarios_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageUsuarios);
            ResetarMenus();
            btnMenuUsuarios.Style = (Style)FindResource("MenuButtonActive");
            MostrarCarregando("Carregando usuários...");
            try {
                var lista = await Task.Run(() => _usuarioService.ObterTodos().ToList());
                _todosUsuarios = lista.Select(u => new UsuarioViewModel {
                    Id = u.Id, Nome = u.Nome, Perfil = u.Perfil,
                    Foto = u.Foto, IsCurrentUser = u.Id == _usuarioLogado.Id
                }).ToList();
                txtBuscaUsuario.Text = string.Empty;
                AplicarFiltroUsuarios();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar usuários:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            } finally {
                EsconderCarregando();
            }
        }

        // ── Usuários ─────────────────────────────────────────────────

        private List<UsuarioViewModel> _todosUsuarios = new();

        private void CarregarUsuarios() {
            _todosUsuarios = _usuarioService.ObterTodos()
                .Select(u => new UsuarioViewModel {
                    Id            = u.Id,
                    Nome          = u.Nome,
                    Perfil        = u.Perfil,
                    Foto          = u.Foto,
                    IsCurrentUser = u.Id == _usuarioLogado.Id
                }).ToList();
            AplicarFiltroUsuarios();
        }

        private void AplicarFiltroUsuarios() {
            var busca = txtBuscaUsuario.Text.Trim().ToLower();
            var filtrado = string.IsNullOrEmpty(busca)
                ? _todosUsuarios
                : _todosUsuarios.Where(u => u.Nome.ToLower().Contains(busca)).ToList();
            dgUsuarios.ItemsSource = filtrado;
            var total = _todosUsuarios.Count;
            txtUsuarioContador.Text = total == 1 ? "1 usuário" : $"{total} usuários";
        }

        private void TxtBuscaUsuario_TextChanged(object sender, TextChangedEventArgs e) {
            AplicarFiltroUsuarios();
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
            try {
                _todosProdutos = _produtoService.ObterTodos()
                    .Select(p => new ProdutoViewModel {
                        Id           = p.Id,
                        Nome         = p.Nome ?? string.Empty,
                        Descricao    = p.Descricao,
                        CodigoBarras = p.CodigoBarras,
                        Preco        = p.Preco,
                        Desconto     = p.Desconto,
                        Estoque      = p.Estoque,
                        Ativo        = p.Ativo,
                        Foto         = p.Foto
                    }).ToList();

                AplicarFiltroProdutos();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao recarregar produtos:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltroProdutos() {
            var busca = txtBuscaProduto?.Text.Trim().ToLower() ?? string.Empty;

            var filtrado = _todosProdutos
                .Where(p => !_filtroSoAtivos || p.Ativo)
                .Where(p => string.IsNullOrEmpty(busca)
                         || (p.Nome?.ToLower().Contains(busca) ?? false)
                         || (p.CodigoBarras?.ToLower().Contains(busca) ?? false))
                .ToList();

            dgProdutos.ItemsSource   = filtrado;
            var total = _todosProdutos.Count;
            if (txtContadorProdutos != null)
                txtContadorProdutos.Text = total == 1 ? "1 produto" : $"{total} produtos";
            if (txtFiltroAtivo != null)
                txtFiltroAtivo.Text = _filtroSoAtivos ? "Somente ativos ✔" : "Todos";
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
            try {
                if (((Button)sender).Tag is ProdutoViewModel vm) {
                    var produto = _produtoService.ObterPorId(vm.Id);
                    if (produto == null) return;
                    var janela = new CadastroProdutoWindow(produto);
                    if (janela.ShowDialog() == true)
                        CarregarProdutos();
                }
            } catch (Exception ex) {
                MessageBox.Show("Erro ao abrir edição:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExcluirProduto_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is not ProdutoViewModel vm) return;

            if (_produtoService.TemPedidos(vm.Id)) {
                var desativar = MessageBox.Show(
                    $"O produto \"{vm.Nome}\" possui pedidos e não pode ser excluído.\n\nDeseja desativá-lo em vez disso?",
                    "Produto com pedidos", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (desativar == MessageBoxResult.Yes) {
                    var produto = _produtoService.ObterPorId(vm.Id);
                    if (produto == null) return;
                    produto.Ativo = false;
                    _produtoService.Atualizar(produto);
                    CarregarProdutos();
                }
                return;
            }

            var confirm = MessageBox.Show(
                $"Excluir o produto \"{vm.Nome}\"?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes) {
                _produtoService.Excluir(vm.Id);
                CarregarProdutos();
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
                    .Where(p => p.Ativo)
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
                pdvBtnAdicionar.IsEnabled = resultado[0].Estoque > 0;
            } else {
                var selecionado = pdvLstProdutos.SelectedItem as ProdutoViewModel;
                pdvBtnAdicionar.IsEnabled = selecionado != null && selecionado.Estoque > 0;
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
            var selecionado = pdvLstProdutos.SelectedItem as ProdutoViewModel;
            pdvBtnAdicionar.IsEnabled = selecionado != null && selecionado.Estoque > 0;
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
                    Nome          = produto.Desconto > 0
                                        ? $"{produto.Nome} (-{produto.Desconto:F0}%)"
                                        : produto.Nome,
                    PrecoUnitario = produto.PrecoComDesconto,
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

            var totalValor = _carrinho.Sum(i => i.Subtotal);

            var janela = new Views.PagamentoWindow(totalValor) { Owner = this };
            if (janela.ShowDialog() != true) return;

            try {
                var itens = _carrinho.Select(c => new PedidoItem {
                    ProdutoId     = c.ProdutoId,
                    NomeProduto   = c.Nome,
                    Quantidade    = c.Quantidade,
                    PrecoUnitario = c.PrecoUnitario,
                    Subtotal      = c.Subtotal
                }).ToList();

                var pedido = _pedidoService.Finalizar(_usuarioLogado.Id, itens, janela.FormaSelecionada);

                // Registra entrada no caixa para pagamentos em dinheiro
                var valorDinheiro = janela.Pagamentos
                    .Where(p => p.Forma == "dinheiro")
                    .Sum(p => p.Valor) - janela.Troco;
                if (valorDinheiro > 0)
                    CaixaRegistrarVenda(valorDinheiro, pedido.Id, pedido.Numero);

                _carrinho.Clear();
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
                PdvCarregarProdutos();

                var ptBR     = new System.Globalization.CultureInfo("pt-BR");
                var temTroco = janela.Troco > 0 && janela.Pagamentos.Any(p => p.Forma == "dinheiro");
                var msg      = temTroco
                    ? $"Pedido finalizado com sucesso!\n\n💵 Troco: {janela.Troco.ToString("C2", ptBR)}"
                    : "Pedido finalizado com sucesso!";

                MessageBox.Show(msg, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao finalizar pedido:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PdvAtualizarTotal() {
            var total   = _carrinho.Sum(i => i.Subtotal);
            var qtdItens = _carrinho.Sum(i => i.Quantidade);
            var ptBR    = new System.Globalization.CultureInfo("pt-BR");

            pdvTxtTotal.Text      = total.ToString("C2", ptBR);
            pdvTxtTotalItens.Text = qtdItens == 1 ? "1 item" : $"{qtdItens} itens";

            // Badge no cabeçalho do carrinho
            if (qtdItens > 0) {
                pdvTxtBadgeItens.Text    = qtdItens.ToString();
                pdvBadgeItens.Visibility = Visibility.Visible;
            } else {
                pdvBadgeItens.Visibility = Visibility.Collapsed;
            }
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

        // ── Pedidos ──────────────────────────────────────────────────

        private void PedidosCarregarRecentes() {
            try {
                var lista = _pedidoRepository.ListarRecentes(30).ToList();
                dgPedidos.ItemsSource          = lista;
                dgItensPedido.ItemsSource      = null;
                pedidosTxtContador.Text        = $"{lista.Count} pedido(s) recente(s)";
                pedidosTxtDetalhe.Text         = "Selecione um pedido para ver os itens";
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar pedidos:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PedidosBuscar() {
            try {
                List<PedidoResumoViewModel> resultado;

                if (!string.IsNullOrEmpty(_pedidoNumeroBuffer)
                    && int.TryParse(_pedidoNumeroBuffer, out var num)) {
                    resultado = _pedidoRepository.BuscarPorNumero(num).ToList();
                } else if (!string.IsNullOrEmpty(pedidosTxtBuscaId.Text.Trim())) {
                    var texto = pedidosTxtBuscaId.Text.Trim();
                    if (!Guid.TryParse(texto, out var guid)) {
                        MessageBox.Show("ID inválido. Digite um UUID completo ou use o teclado numérico.",
                            "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    resultado = _pedidoRepository.BuscarPorId(guid).ToList();
                } else {
                    PedidosCarregarRecentes();
                    return;
                }

                dgPedidos.ItemsSource   = resultado;
                pedidosTxtContador.Text = $"{resultado.Count} pedido(s) encontrado(s)";

                // Seleciona automaticamente e carrega os itens
                if (resultado.Count > 0) {
                    dgPedidos.SelectedIndex = 0;
                    dgPedidos.ScrollIntoView(dgPedidos.SelectedItem);
                } else {
                    dgItensPedido.ItemsSource = null;
                    pedidosTxtDetalhe.Text    = "Nenhum pedido encontrado";
                }
            } catch (Exception ex) {
                MessageBox.Show("Erro na busca:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Teclado físico capturado na janela inteira ────────────────
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (pagePedidos.Visibility != Visibility.Visible) return;
            // Se um TextBox (campo UUID) está com foco, não interceptar
            if (Keyboard.FocusedElement is System.Windows.Controls.TextBox) return;

            if (e.Key >= Key.D0 && e.Key <= Key.D9) {
                PedidosAdicionarDigito((e.Key - Key.D0).ToString());
                e.Handled = true;
            } else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) {
                PedidosAdicionarDigito((e.Key - Key.NumPad0).ToString());
                e.Handled = true;
            } else if (e.Key == Key.Back || e.Key == Key.Delete) {
                PedidosRemoverUltimoDigito();
                e.Handled = true;
            } else if (e.Key == Key.Enter) {
                PedidosBuscar();
                e.Handled = true;
            }
        }

        private void PedidosAdicionarDigito(string digit) {
            if (_pedidoNumeroBuffer.Length >= 8) return;
            _pedidoNumeroBuffer   += digit;
            pedidosTxtNumero.Text = _pedidoNumeroBuffer;
        }

        private void PedidosRemoverUltimoDigito() {
            if (_pedidoNumeroBuffer.Length == 0) return;
            _pedidoNumeroBuffer   = _pedidoNumeroBuffer[..^1];
            pedidosTxtNumero.Text = _pedidoNumeroBuffer;
        }

        private void PedidosNumpad_Click(object sender, RoutedEventArgs e) {
            if (((Button)sender).Tag is string digit)
                PedidosAdicionarDigito(digit);
        }

        private void PedidosNumpadDel_Click(object sender, RoutedEventArgs e)
            => PedidosRemoverUltimoDigito();

        private void PedidosNumpadBuscar_Click(object sender, RoutedEventArgs e)
            => PedidosBuscar();

        private void PedidosTxtBuscaId_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) PedidosBuscar();
        }

        private void DgPedidos_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (dgPedidos.SelectedItem is not PedidoResumoViewModel pedido) return;
            try {
                var itens = _pedidoRepository.ObterItens(pedido.Id).ToList();
                dgItensPedido.ItemsSource      = itens;
                pedidosTxtDetalhe.Text         = $"Itens do Pedido {pedido.NumeroTexto}  —  {pedido.TotalTexto}  |  {pedido.PagamentoTexto}";
                pedidosItensVazio.Visibility   = itens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar itens:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PedidosBtnLimpar_Click(object sender, RoutedEventArgs e) {
            _pedidoNumeroBuffer       = string.Empty;
            pedidosTxtNumero.Text     = string.Empty;
            pedidosTxtBuscaId.Text    = string.Empty;
            PedidosCarregarRecentes();
        }

        // ── Sair ─────────────────────────────────────────────────────

        private void BtnSair_Click(object sender, RoutedEventArgs e) {
            new LoginWindow().Show();
            this.Close();
        }

        // ════════════════════════════════════════════════════════════
        // ── CAIXA ────────────────────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuCaixa_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageCaixa);
            ResetarMenus();
            btnMenuCaixa.Style = (Style)FindResource("MenuButtonActive");
            CaixaCarregar();
        }

        private void CaixaCarregar() {
            try {
                _caixaAtivo = _caixaService.ObterCaixaAberto();
                if (_caixaAtivo == null) {
                    caixaPanelFechado.Visibility = Visibility.Visible;
                    caixaPanelAberto.Visibility  = Visibility.Collapsed;
                } else {
                    CaixaCarregarDados();
                }
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar caixa:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CaixaCarregarDados() {
            if (_caixaAtivo == null) return;

            caixaPanelFechado.Visibility = Visibility.Collapsed;
            caixaPanelAberto.Visibility  = Visibility.Visible;

            var ptBR = new System.Globalization.CultureInfo("pt-BR");
            caixaTxtAbertura.Text = $"Aberto em {_caixaAtivo.DataAbertura:dd/MM/yyyy} às {_caixaAtivo.DataAbertura:HH:mm}  •  Saldo inicial: {_caixaAtivo.SaldoInicial.ToString("C2", ptBR)}";

            var (saldo, entradas, saidas) = _caixaService.ObterResumo(_caixaAtivo.Id);
            caixaTxtSaldo.Text    = saldo.ToString("C2", ptBR);
            caixaTxtEntradas.Text = entradas.ToString("C2", ptBR);
            caixaTxtSaidas.Text   = saidas.ToString("C2", ptBR);

            var movs = _caixaService.ListarMovimentacoes(_caixaAtivo.Id)
                .Select(m => new MovimentacaoViewModel {
                    Id        = m.Id,
                    Tipo      = m.Tipo,
                    Descricao = m.Descricao,
                    Valor     = m.Valor,
                    Data      = m.Data,
                    Origem    = m.Origem
                }).ToList();

            caixaDgMovimentacoes.ItemsSource = movs;
            caixaEmptyState.Visibility = movs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAbrirCaixa_Click(object sender, RoutedEventArgs e) {
            var janela = new AbrirCaixaWindow { Owner = this };
            if (janela.ShowDialog() != true) return;
            try {
                _caixaAtivo = _caixaService.AbrirCaixa(janela.SaldoInicial, _usuarioLogado.Id);
                CaixaCarregarDados();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao abrir caixa:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFecharCaixa_Click(object sender, RoutedEventArgs e) {
            if (_caixaAtivo == null) return;
            try {
                var (_, entradas, saidas) = _caixaService.ObterResumo(_caixaAtivo.Id);
                var movimentacoes         = _caixaService.ListarMovimentacoes(_caixaAtivo.Id);

                var janela = new Views.FechamentoCaixaWindow(
                    _caixaAtivo, _usuarioLogado.Nome, entradas, saidas, movimentacoes) { Owner = this };

                if (janela.ShowDialog() != true) return;

                _caixaService.FecharCaixa(_caixaAtivo.Id);
                _caixaAtivo = null;
                caixaPanelAberto.Visibility  = Visibility.Collapsed;
                caixaPanelFechado.Visibility = Visibility.Visible;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao fechar caixa:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNovaEntrada_Click(object sender, RoutedEventArgs e) {
            if (_caixaAtivo == null) return;
            var janela = new MovimentacaoWindow("entrada") { Owner = this };
            if (janela.ShowDialog() != true) return;
            try {
                _caixaService.RegistrarEntrada(_caixaAtivo.Id, janela.Descricao, janela.Valor);
                CaixaCarregarDados();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao registrar entrada:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNovaSaida_Click(object sender, RoutedEventArgs e) {
            if (_caixaAtivo == null) return;
            var janela = new MovimentacaoWindow("saida") { Owner = this };
            if (janela.ShowDialog() != true) return;
            try {
                _caixaService.RegistrarSaida(_caixaAtivo.Id, janela.Descricao, janela.Valor);
                CaixaCarregarDados();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao registrar saída:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnHistoricoCaixa_Click(object sender, RoutedEventArgs e) {
            var janela = new Views.HistoricoCaixaWindow { Owner = this };
            janela.ShowDialog();
        }

        private void BtnVoltarCaixa_Click(object sender, RoutedEventArgs e) {
            caixaPanelHistorico.Visibility = Visibility.Collapsed;
            CaixaCarregar();
        }

        // Chamado após finalizar pedido com dinheiro — registra entrada no caixa automaticamente
        private void CaixaRegistrarVenda(decimal valorDinheiro, Guid pedidoId, int numeroPedido) {
            if (_caixaAtivo == null) return;
            try {
                _caixaService.RegistrarEntrada(
                    _caixaAtivo.Id,
                    $"Venda #{numeroPedido:D4}",
                    valorDinheiro,
                    pedidoId,
                    "venda");
            } catch {
                // Não interrompe o fluxo se o caixa não estiver aberto
            }
        }
    }
}
