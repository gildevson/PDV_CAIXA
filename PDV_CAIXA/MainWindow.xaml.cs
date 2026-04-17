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
        private readonly Usuario                  _usuarioLogado;
        private readonly UsuarioService           _usuarioService           = new();
        private readonly ProdutoService           _produtoService           = new();
        private readonly PedidoService            _pedidoService            = new();
        private readonly PedidoRepository         _pedidoRepository         = new();
        private readonly CaixaService             _caixaService             = new();
        private readonly RelatorioService         _relatorioService         = new();
        private readonly ContaContabilRepository  _contaContabilRepository  = new();
        private List<ProdutoViewModel>            _todosProdutos            = new();
        private bool                              _filtroSoAtivos           = false;
        private List<Models.ContaContabil>        _todasContas              = new();

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

                histDgPedidos.ItemsSource    = lista;
                histDgItens.ItemsSource     = null;
                histTxtDetalhe.Text         = "Selecione um pedido para ver os itens";
                btnImprimirPedido.Visibility = Visibility.Collapsed;
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
            if (histDgPedidos.SelectedItem is not PedidoResumoViewModel pedido) {
                btnImprimirPedido.Visibility = Visibility.Collapsed;
                return;
            }
            try {
                histDgItens.ItemsSource      = _pedidoRepository.ObterItens(pedido.Id).ToList();
                histTxtDetalhe.Text          = $"Itens do Pedido {pedido.NumeroTexto}  —  {pedido.TotalTexto}";
                btnImprimirPedido.Visibility = Visibility.Visible;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar itens:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImprimirPedido_Click(object sender, RoutedEventArgs e) {
            if (histDgPedidos.SelectedItem is not PedidoResumoViewModel pedido) return;
            try {
                var itens = _pedidoRepository.ObterItens(pedido.Id).ToList();
                _relatorioService.ImprimirHistoricoPedido(pedido, itens);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao gerar relatório:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Pedidos (pesquisa) ────────────────────────────────────────
        private string _pedidoNumeroBuffer = string.Empty;

        public MainWindow(Usuario usuario) {
            InitializeComponent();
            _usuarioLogado = usuario;
            ConfigurarPerfil();
            // Carrega caixa aberto imediatamente para que vendas no PDV possam ser finalizadas
            // mesmo sem o usuário ter visitado a página de Caixa nesta sessão
            try { _caixaAtivo = _caixaService.ObterCaixaAberto(); } catch { }
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
            pageInicio.Visibility         = Visibility.Collapsed;
            pagePDV.Visibility            = Visibility.Collapsed;
            pageProdutos.Visibility       = Visibility.Collapsed;
            pageUsuarios.Visibility       = Visibility.Collapsed;
            pagePedidos.Visibility        = Visibility.Collapsed;
            pageHistorico.Visibility      = Visibility.Collapsed;
            pageCaixa.Visibility          = Visibility.Collapsed;
            pageRelatorios.Visibility         = Visibility.Collapsed;
            pageConfigRelatorios.Visibility   = Visibility.Collapsed;
            pageEmpresa.Visibility            = Visibility.Collapsed;
            pageFerramentas.Visibility        = Visibility.Collapsed;
            pageContaContabil.Visibility      = Visibility.Collapsed;
            pagina.Visibility             = Visibility.Visible;
        }

        private void ResetarMenus() {
            btnMenuInicio.Style         = (Style)FindResource("MenuButton");
            btnMenuPDV.Style            = (Style)FindResource("MenuButton");
            btnMenuProdutos.Style       = (Style)FindResource("MenuButton");
            btnMenuUsuarios.Style       = (Style)FindResource("MenuButton");
            btnMenuPedidos.Style        = (Style)FindResource("MenuButton");
            btnMenuHistorico.Style      = (Style)FindResource("MenuButton");
            btnMenuCaixa.Style          = (Style)FindResource("MenuButton");
            btnMenuRelatorios.Style     = (Style)FindResource("MenuButton");
            btnMenuEmpresa.Style        = (Style)FindResource("MenuButton");
            btnMenuFerramentas.Style    = (Style)FindResource("MenuButton");
            btnMenuContaContabil.Style  = (Style)FindResource("MenuButton");
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
                            Preco = p.Preco, Desconto = p.Desconto, Estoque = p.Estoque,
                            Ativo = p.Ativo, VendidoPorPeso = p.VendidoPorPeso
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
                histDgPedidos.ItemsSource    = lista;
                histDgItens.ItemsSource     = null;
                histTxtDetalhe.Text         = "Selecione um pedido para ver os itens";
                btnImprimirPedido.Visibility = Visibility.Collapsed;
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
                        Id             = p.Id,
                        Nome           = p.Nome,
                        CodigoBarras   = p.CodigoBarras,
                        Preco          = p.Preco,
                        Desconto       = p.Desconto,
                        Estoque        = p.Estoque,
                        Ativo          = p.Ativo,
                        VendidoPorPeso = p.VendidoPorPeso
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

            if (produto.VendidoPorPeso) {
                var dialog = new PesarProdutoWindow(produto) { Owner = this };
                if (dialog.ShowDialog() != true) return;

                var pesoKg     = dialog.PesoKg;
                var pesoTexto  = pesoKg.ToString("N3", new System.Globalization.CultureInfo("pt-BR"));
                var nomeItem   = produto.Desconto > 0
                    ? $"{produto.Nome} ({pesoTexto} kg) (-{produto.Desconto:F0}%)"
                    : $"{produto.Nome} ({pesoTexto} kg)";

                _carrinho.Add(new CarrinhoItemViewModel {
                    ProdutoId     = produto.Id,
                    Nome          = nomeItem,
                    PrecoUnitario = dialog.Total,
                    Quantidade    = 1,
                    Peso          = dialog.PesoKg
                });
            } else {
                if (!int.TryParse(pdvTxtQtd.Text, out var qtd) || qtd < 1)
                    qtd = 1;

                var existente = _carrinho.FirstOrDefault(c => c.ProdutoId == produto.Id && !c.Nome.Contains(" kg)"));
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
            }

            pdvTxtQtd.Text              = "1";
            pdvLstProdutos.SelectedItem = null;
            pdvTxtBusca.Text            = string.Empty;
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

            // Caixa deve estar aberto para registrar os movimentos da venda
            if (_caixaAtivo == null) {
                MessageBox.Show(
                    "Nenhum caixa aberto.\n\nAbra o caixa antes de finalizar uma venda.",
                    "Caixa Fechado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var totalValor = _carrinho.Sum(i => i.Subtotal);

            var janela = new Views.PagamentoWindow(totalValor) { Owner = this };
            if (janela.ShowDialog() != true) return;

            try {
                var itens = _carrinho.Select(c => new PedidoItem {
                    ProdutoId     = c.ProdutoId,
                    NomeProduto   = c.Nome,
                    Quantidade    = c.Quantidade,
                    Peso          = c.Peso,
                    PrecoUnitario = c.PrecoUnitario,
                    Subtotal      = c.Subtotal
                }).ToList();

                var pedido = _pedidoService.Finalizar(_usuarioLogado.Id, itens, janela.FormaSelecionada);

                // Registra TODAS as formas de pagamento no caixa
                foreach (var pag in janela.Pagamentos) {
                    // Para dinheiro: desconta o troco (valor líquido que ficou no caixa)
                    var valorLiquido = pag.Forma == "dinheiro"
                        ? pag.Valor - janela.Troco
                        : pag.Valor;
                    if (valorLiquido > 0)
                        CaixaRegistrarVenda(valorLiquido, pag.Forma, pedido.Id, pedido.Numero);
                }

                _carrinho.Clear();
                PdvAtualizarTotal();
                PdvAtualizarVisibilidade();
                PdvCarregarProdutos();

                // Atualiza o painel de caixa para refletir os novos movimentos
                if (_caixaAtivo != null && caixaPanelAberto.Visibility == Visibility.Visible)
                    CaixaCarregarDados();

                var ptBR     = new System.Globalization.CultureInfo("pt-BR");
                var temTroco = janela.Troco > 0 && janela.Pagamentos.Any(p => p.Forma == "dinheiro");
                var msg      = temTroco
                    ? $"Pedido finalizado com sucesso!\n\n💵 Troco: {janela.Troco.ToString("C2", ptBR)}"
                    : "Pedido finalizado com sucesso!";

                var itensPedido = _pedidoRepository.ObterItens(pedido.Id).ToList();
                var pagsPedido  = janela.Pagamentos.Select(p => new PagamentoCupom(p.Forma, p.Valor)).ToList();

                var reciboResult = MessageBox.Show(
                    msg + "\n\nDeseja imprimir o Recibo de Venda?",
                    "Sucesso", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (reciboResult == MessageBoxResult.Yes)
                    _relatorioService.GerarReciboPedido(pedido, itensPedido, pagsPedido, janela.Troco, _usuarioLogado.Nome);
            } catch (Exception ex) {
                var sb = new System.Text.StringBuilder();
                var exc = ex;
                while (exc != null) { sb.AppendLine(exc.Message); exc = exc.InnerException; }
                MessageBox.Show("Erro ao finalizar pedido:\n\n" + sb,
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

            if (e.Key == Key.F2) {
                pdvTxtBusca.Focus();
                pdvTxtBusca.SelectAll();
                e.Handled = true;
            } else if (e.Key == Key.F12) {
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
        private bool _telaCheia = false;

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.F11) {
                if (!_telaCheia) {
                    WindowStyle  = WindowStyle.None;
                    WindowState  = WindowState.Maximized;
                    _telaCheia   = true;
                } else {
                    WindowStyle  = WindowStyle.SingleBorderWindow;
                    WindowState  = WindowState.Normal;
                    _telaCheia   = false;
                }
                e.Handled = true;
                return;
            }

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
            if (dgPedidos.SelectedItem is not PedidoResumoViewModel pedido) {
                btnImprimirReciboPesquisa.Visibility = Visibility.Collapsed;
                return;
            }
            try {
                var itens = _pedidoRepository.ObterItens(pedido.Id).ToList();
                dgItensPedido.ItemsSource            = itens;
                pedidosTxtDetalhe.Text               = $"Itens do Pedido {pedido.NumeroTexto}  —  {pedido.TotalTexto}  |  {pedido.PagamentoTexto}";
                pedidosItensVazio.Visibility         = itens.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                btnImprimirReciboPesquisa.Visibility = itens.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar itens:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImprimirReciboPesquisa_Click(object sender, RoutedEventArgs e) {
            if (dgPedidos.SelectedItem is not PedidoResumoViewModel pedido) return;
            try {
                var itens = _pedidoRepository.ObterItens(pedido.Id).ToList();
                _relatorioService.ImprimirHistoricoPedido(pedido, itens);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao imprimir recibo:\n\n" + ex.Message,
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

        // ════════════════════════════════════════════════════════════
        // ── RELATÓRIOS ───────────────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuRelatorios_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageRelatorios);
            ResetarMenus();
            btnMenuRelatorios.Style = (Style)FindResource("MenuButtonActive");
            RelCarregarComboBox();
            RelCarregarLista();
        }

        private void RelCarregarComboBox() {
            try {
                var configs = new Repositories.RelatorioConfigRepository().ObterAtivos().ToList();
                relCmbTipo.ItemsSource       = configs;
                relCmbTipo.DisplayMemberPath = "Nome";
                relCmbTipo.SelectedIndex     = -1;
                relCmbPlaceholder.Visibility     = Visibility.Visible;
                btnRelGerarSelecionado.IsEnabled = false;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar relatórios:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RelCmbTipo_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (relCmbTipo.SelectedItem != null) {
                relCmbPlaceholder.Visibility     = Visibility.Collapsed;
                btnRelGerarSelecionado.IsEnabled = true;
            } else {
                relCmbPlaceholder.Visibility     = Visibility.Visible;
                btnRelGerarSelecionado.IsEnabled = false;
            }
        }

        private void BtnRelGerarSelecionado_Click(object sender, RoutedEventArgs e) {
            if (relCmbTipo.SelectedItem is not Models.RelatorioConfig config) return;
            try {
                switch (config.Tipo) {
                    case "Usuarios":
                        var usuarios = new Repositories.UsuarioRepository().ObterTodos().ToList();
                        _relatorioService.GerarRelatorioUsuarios(usuarios, config.NomeArquivo);
                        break;
                    case "Produtos":
                        var produtos = new Repositories.ProdutoRepository().ObterTodos().ToList();
                        _relatorioService.GerarRelatorioProdutos(produtos, config.NomeArquivo);
                        break;
                    case "ProdutosAtivos":
                        _relatorioService.GerarRelatorioProdutosAtivos(config.NomeArquivo);
                        break;
                    default:
                        MessageBox.Show($"Tipo '{config.Tipo}' não possui gerador configurado.",
                            "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                }
                RelCarregarLista();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao gerar relatório:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // ── CONFIG. RELATÓRIOS ───────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuConfigRelatorios_Click(object sender, RoutedEventArgs e) {
            var resp = MessageBox.Show(
                "Tem certeza que deseja acessar a configuração de relatórios?\n\nAlterações aqui afetam os relatórios disponíveis para impressão.",
                "Configuração de Relatórios",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resp != MessageBoxResult.Yes) return;

            MostrarPagina(pageConfigRelatorios);
            ResetarMenus();
            btnMenuConfigRelatorios.Style = (Style)FindResource("MenuButtonActive");
            CfgRelCarregar();
        }

        private void CfgRelCarregar() {
            try {
                cfgRelDgConfigs.ItemsSource = new Repositories.RelatorioConfigRepository().ObterTodos().ToList();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar relatórios:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConfigRelNovo_Click(object sender, RoutedEventArgs e) {
            var win = new Views.CadastroRelatorioWindow { Owner = this };
            if (win.ShowDialog() == true) CfgRelCarregar();
        }

        private void BtnConfigRelEditar_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button btn || btn.Tag is not Models.RelatorioConfig config) return;
            var win = new Views.CadastroRelatorioWindow(config) { Owner = this };
            if (win.ShowDialog() == true) CfgRelCarregar();
        }

        private void BtnConfigRelExcluir_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button btn || btn.Tag is not Models.RelatorioConfig config) return;
            var resp = MessageBox.Show($"Excluir o relatório:\n{config.Nome}?",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp != MessageBoxResult.Yes) return;
            try {
                new Repositories.RelatorioConfigRepository().Excluir(config.Id);
                CfgRelCarregar();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao excluir:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RelCarregarLista() {
            var pasta = RelatorioService.PastaRelatorios();
            System.IO.Directory.CreateDirectory(pasta); // garante que a pasta sempre existe

            var arquivosExistentes = System.IO.Directory.GetFiles(pasta, "*.pdf");
            if (arquivosExistentes.Length == 0) {
                relDgArquivos.ItemsSource = null;
                relPanelVazio.Visibility  = Visibility.Visible;
                relDgArquivos.Visibility  = Visibility.Collapsed;
                return;
            }

            var arquivos = new System.IO.DirectoryInfo(pasta)
                .GetFiles("*.pdf")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => new RelatorioArquivoViewModel {
                    Nome            = f.Name,
                    Tamanho         = f.Length < 1024 ? $"{f.Length} B"
                                    : f.Length < 1024 * 1024 ? $"{f.Length / 1024} KB"
                                    : $"{f.Length / (1024 * 1024)} MB",
                    DataTexto       = f.LastWriteTime.ToString("dd/MM/yyyy HH:mm"),
                    CaminhoCompleto = f.FullName
                })
                .ToList();

            if (arquivos.Count == 0) {
                relDgArquivos.ItemsSource = null;
                relPanelVazio.Visibility  = Visibility.Visible;
                relDgArquivos.Visibility  = Visibility.Collapsed;
            } else {
                relDgArquivos.ItemsSource = arquivos;
                relDgArquivos.Visibility  = Visibility.Visible;
                relPanelVazio.Visibility  = Visibility.Collapsed;
            }
        }

        private void BtnRelAtualizar_Click(object sender, RoutedEventArgs e)
            => RelCarregarLista();

        private void RelDgArquivos_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (relDgArquivos.SelectedItem is RelatorioArquivoViewModel item)
                AbrirRelatorio(item.CaminhoCompleto);
        }

        private void BtnRelAbrir_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn && btn.Tag is string caminho)
                AbrirRelatorio(caminho);
        }

        private void BtnRelExcluir_Click(object sender, RoutedEventArgs e) {
            if (sender is not Button btn || btn.Tag is not string caminho) return;
            var nome = System.IO.Path.GetFileName(caminho);
            var resp = MessageBox.Show($"Excluir o arquivo:\n{nome}?",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resp != MessageBoxResult.Yes) return;
            try {
                System.IO.File.Delete(caminho);
                RelCarregarLista();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao excluir:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void AbrirRelatorio(string caminho) {
            if (!System.IO.File.Exists(caminho)) {
                MessageBox.Show("Arquivo não encontrado:\n" + caminho,
                    "Arquivo não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var titulo = System.IO.Path.GetFileNameWithoutExtension(caminho);
            var preview = new Views.RelatorioPreviewWindow(titulo, caminho);
            preview.Show();
        }

        // ════════════════════════════════════════════════════════════
        // ── MINHA EMPRESA ────────────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuEmpresa_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageEmpresa);
            ResetarMenus();
            btnMenuEmpresa.Style = (Style)FindResource("MenuButtonActive");
            EmpresaCarregar();
        }

        private void EmpresaCarregar() {
            try {
                var emp = new Repositories.EmpresaRepository().Obter();
                empEdtRazaoSocial.Text   = emp.RazaoSocial;
                empEdtNomeFantasia.Text  = emp.NomeFantasia;
                empEdtCnpj.Text         = emp.Cnpj;
                empEdtInscricaoEst.Text  = emp.InscricaoEst;
                empEdtTelefone.Text      = emp.Telefone;
                empEdtEmail.Text         = emp.Email;
                empEdtWebsite.Text       = emp.Website;
                empEdtCep.Text           = emp.Cep;
                empEdtLogradouro.Text    = emp.Logradouro;
                empEdtNumero.Text        = emp.Numero;
                empEdtComplemento.Text   = emp.Complemento;
                empEdtBairro.Text        = emp.Bairro;
                empEdtCidade.Text        = emp.Cidade;
                empEdtUf.Text            = emp.Uf;
                empBannerSucesso.Visibility = Visibility.Collapsed;
                empBannerErro.Visibility    = Visibility.Collapsed;
            } catch (Exception ex) {
                empBannerErro.Visibility = Visibility.Visible;
                empTxtErro.Text = "Erro ao carregar dados: " + ex.Message;
            }
        }

        private void BtnEmpresaSalvar_Click(object sender, RoutedEventArgs e) {
            empBannerSucesso.Visibility = Visibility.Collapsed;
            empBannerErro.Visibility    = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(empEdtRazaoSocial.Text)) {
                empBannerErro.Visibility = Visibility.Visible;
                empTxtErro.Text = "Razão Social é obrigatória.";
                return;
            }
            if (string.IsNullOrWhiteSpace(empEdtCnpj.Text)) {
                empBannerErro.Visibility = Visibility.Visible;
                empTxtErro.Text = "CNPJ é obrigatório.";
                return;
            }

            try {
                var emp = new Repositories.EmpresaRepository().Obter();
                emp.RazaoSocial  = empEdtRazaoSocial.Text.Trim();
                emp.NomeFantasia = empEdtNomeFantasia.Text.Trim();
                emp.Cnpj         = empEdtCnpj.Text.Trim();
                emp.InscricaoEst = empEdtInscricaoEst.Text.Trim();
                emp.Telefone     = empEdtTelefone.Text.Trim();
                emp.Email        = empEdtEmail.Text.Trim();
                emp.Website      = empEdtWebsite.Text.Trim();
                emp.Cep          = empEdtCep.Text.Trim();
                emp.Logradouro   = empEdtLogradouro.Text.Trim();
                emp.Numero       = empEdtNumero.Text.Trim();
                emp.Complemento  = empEdtComplemento.Text.Trim();
                emp.Bairro       = empEdtBairro.Text.Trim();
                emp.Cidade       = empEdtCidade.Text.Trim();
                emp.Uf           = empEdtUf.Text.Trim().ToUpper();

                new Repositories.EmpresaRepository().Salvar(emp);
                empBannerSucesso.Visibility = Visibility.Visible;
            } catch (Exception ex) {
                empBannerErro.Visibility = Visibility.Visible;
                empTxtErro.Text = "Erro ao salvar: " + ex.Message;
            }
        }

        // ════════════════════════════════════════════════════════════
        // ── CONTA CONTÁBIL ───────────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuContaContabil_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageContaContabil);
            ResetarMenus();
            btnMenuContaContabil.Style = (Style)FindResource("MenuButtonActive");
            ContaCarregar();
        }

        private void ContaCarregar() {
            try {
                _todasContas = _contaContabilRepository.ObterTodos().ToList();
                ContaAplicarFiltro(txtBuscaConta.Text);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar contas contábeis:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContaAplicarFiltro(string busca) {
            var filtro = string.IsNullOrWhiteSpace(busca)
                ? _todasContas
                : _todasContas.Where(c =>
                    c.CodigoContabil.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    c.Descricao.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (c.CodigoReduzido?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true)).ToList();

            dgContas.ItemsSource    = filtro;
            txtContadorContas.Text  = $"{filtro.Count} conta(s)";
        }

        private void TxtBuscaConta_TextChanged(object sender, TextChangedEventArgs e)
            => ContaAplicarFiltro(txtBuscaConta.Text);

        private void BtnNovaContaContabil_Click(object sender, RoutedEventArgs e) {
            var janela = new CadastroContaContabilWindow { Owner = this };
            if (janela.ShowDialog() == true) ContaCarregar();
        }

        private void DgContas_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (dgContas.SelectedItem is Models.ContaContabil conta) AbrirEdicaoConta(conta);
        }

        private void BtnEditarConta_Click(object sender, RoutedEventArgs e) {
            if (((FrameworkElement)sender).Tag is Models.ContaContabil conta) AbrirEdicaoConta(conta);
        }

        private void AbrirEdicaoConta(Models.ContaContabil conta) {
            var janela = new CadastroContaContabilWindow(conta) { Owner = this };
            if (janela.ShowDialog() == true) ContaCarregar();
        }

        private void BtnExcluirConta_Click(object sender, RoutedEventArgs e) {
            if (((FrameworkElement)sender).Tag is not Models.ContaContabil conta) return;

            var resp = MessageBox.Show(
                $"Deseja excluir a conta \"{conta.CodigoContabil} - {conta.Descricao}\"?",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resp != MessageBoxResult.Yes) return;

            try {
                _contaContabilRepository.Excluir(conta.Id);
                ContaCarregar();
            } catch (Exception ex) {
                MessageBox.Show("Erro ao excluir conta:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // ── FERRAMENTAS ──────────────────────────────────────────────
        // ════════════════════════════════════════════════════════════

        private void BtnMenuFerramentas_Click(object sender, RoutedEventArgs e) {
            MostrarPagina(pageFerramentas);
            ResetarMenus();
            btnMenuFerramentas.Style = (Style)FindResource("MenuButtonActive");
            FerrCarregarInfo();
        }

        private void FerrCarregarInfo() {
            try {
                var cs = Config.AppConfig.ConnectionString;
                ferrEdtHost.Text     = ExtrairParamConn(cs, "Host")     ?? string.Empty;
                ferrEdtPorta.Text    = ExtrairParamConn(cs, "Port")     ?? "5432";
                ferrEdtDatabase.Text = ExtrairParamConn(cs, "Database") ?? string.Empty;
                ferrEdtUsuario.Text  = ExtrairParamConn(cs, "Username") ?? string.Empty;
                ferrEdtSenha.Password = ExtrairParamConn(cs, "Password") ?? string.Empty;
            } catch {
                ferrEdtHost.Text = ferrEdtPorta.Text = ferrEdtDatabase.Text = ferrEdtUsuario.Text = string.Empty;
            }

            ferrEdtPastaRel.Text = RelatorioService.PastaRelatorios();
            ferrTxtExe.Text      = AppDomain.CurrentDomain.BaseDirectory;

            ferrStatusIndicador.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#44446A"));
            ferrTxtStatus.Text       = "Aguardando teste...";
            ferrTxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#44446A"));
        }

        private void BtnFerrSalvar_Click(object sender, RoutedEventArgs e) {
            var host     = ferrEdtHost.Text.Trim();
            var porta    = ferrEdtPorta.Text.Trim();
            var database = ferrEdtDatabase.Text.Trim();
            var usuario  = ferrEdtUsuario.Text.Trim();
            var senha    = ferrEdtSenha.Password;

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database) || string.IsNullOrEmpty(usuario)) {
                MessageBox.Show("Preencha ao menos Servidor, Banco e Usuário.",
                    "Campos obrigatórios", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try {
                Config.AppConfig.Salvar(host, porta, database, usuario, senha);
                MessageBox.Show("Configurações salvas com sucesso!\nA nova conexão será usada a partir de agora.",
                    "Salvo", MessageBoxButton.OK, MessageBoxImage.Information);
            } catch (Exception ex) {
                MessageBox.Show("Erro ao salvar:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string? ExtrairParamConn(string cs, string chave) {
            foreach (var parte in cs.Split(';')) {
                var kv = parte.Trim().Split('=', 2);
                if (kv.Length == 2 && kv[0].Trim().Equals(chave, StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }

        private async void BtnFerrTestarConexao_Click(object sender, RoutedEventArgs e) {
            ferrTxtStatus.Text = "Testando...";
            ferrStatusIndicador.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888800"));
            ferrTxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCC00"));

            var (ok, msg) = await Task.Run(() => {
                try {
                    using var conn = new Data.Conexao().CriarConexao();
                    conn.Open();
                    return (true, $"Conectado com sucesso! ({DateTime.Now:HH:mm:ss})");
                } catch (Exception ex) {
                    return (false, ex.Message);
                }
            });

            if (ok) {
                ferrStatusIndicador.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#00AA44"));
                ferrTxtStatus.Text      = msg;
                ferrTxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#50FA7B"));
            } else {
                ferrStatusIndicador.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#AA2222"));
                ferrTxtStatus.Text      = $"Falha: {msg}";
                ferrTxtStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5555"));
            }
        }

        private void BtnFerrAbrirPasta_Click(object sender, RoutedEventArgs e) {
            var pasta = RelatorioService.PastaRelatorios();
            System.IO.Directory.CreateDirectory(pasta);
            System.Diagnostics.Process.Start("explorer.exe", pasta);
        }


        private void BtnFerrTesteImpressao_Click(object sender, RoutedEventArgs e) {
            try {
                _relatorioService.GerarTesteImpressao();
            } catch (Exception ex) {
                MessageBox.Show("Erro:\n\n" + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    Id             = m.Id,
                    Tipo           = m.Tipo,
                    Descricao      = m.Descricao,
                    Valor          = m.Valor,
                    Data           = m.Data,
                    Origem         = m.Origem,
                    FormaPagamento = m.FormaPagamento
                }).ToList();

            caixaDgMovimentacoes.ItemsSource = movs;
            caixaDetalhePanel.Visibility     = Visibility.Collapsed;
            caixaEmptyState.Visibility       = movs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CaixaDgMovimentacoes_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (caixaDgMovimentacoes.SelectedItem is not MovimentacaoViewModel mov) {
                caixaDetalhePanel.Visibility = Visibility.Collapsed;
                return;
            }

            var ptBR      = new System.Globalization.CultureInfo("pt-BR");
            var isEntrada = mov.Tipo == "entrada";

            caixaDetalheIconeBd.Background = new System.Windows.Media.SolidColorBrush(
                isEntrada
                    ? System.Windows.Media.Color.FromRgb(0x0A, 0x2A, 0x0A)
                    : System.Windows.Media.Color.FromRgb(0x2A, 0x0A, 0x0A));

            caixaDetalheIcone.Text = isEntrada ? "⬆" : "⬇";
            caixaDetalheIcone.Foreground = new System.Windows.Media.SolidColorBrush(
                isEntrada
                    ? System.Windows.Media.Color.FromRgb(0x50, 0xFA, 0x7B)
                    : System.Windows.Media.Color.FromRgb(0xFF, 0x55, 0x55));

            caixaDetalheDescricao.Text = mov.Descricao;
            caixaDetalheData.Text      = mov.Data.ToString("dd/MM/yyyy  HH:mm:ss", ptBR);
            caixaDetalheForma.Text     = mov.FormaTexto;

            caixaDetalheValor.Text = isEntrada
                ? "+ " + mov.Valor.ToString("C2", ptBR)
                : "− " + mov.Valor.ToString("C2", ptBR);
            caixaDetalheValor.Foreground = new System.Windows.Media.SolidColorBrush(
                isEntrada
                    ? System.Windows.Media.Color.FromRgb(0x50, 0xFA, 0x7B)
                    : System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));

            caixaDetalhePanel.Visibility = Visibility.Visible;
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
                var formas        = _caixaService.ObterTotaisPorForma(_caixaAtivo.Id);
                var movimentacoes = _caixaService.ListarMovimentacoes(_caixaAtivo.Id);

                var janela = new Views.FechamentoCaixaWindow(
                    _caixaAtivo, _usuarioLogado.Nome, formas, movimentacoes) { Owner = this };

                if (janela.ShowDialog() != true) return;

                var movsFechamento = _caixaService.ListarMovimentacoes(_caixaAtivo.Id).ToList();
                var formasFechamento = _caixaService.ObterTotaisPorForma(_caixaAtivo.Id);
                var resultado = _caixaService.FecharCaixa(_caixaAtivo.Id, janela.SaldoRealInformado,
                    usuarioFechamentoId: _usuarioLogado.Id);

                var caixaFechada = _caixaAtivo;
                var operador     = _usuarioLogado.Nome;
                _caixaAtivo = null;
                caixaPanelAberto.Visibility  = Visibility.Collapsed;
                caixaPanelFechado.Visibility = Visibility.Visible;

                var printFechamento = MessageBox.Show(
                    "Caixa fechado com sucesso!\n\nDeseja imprimir o relatório de fechamento?",
                    "Fechamento de Caixa", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (printFechamento == MessageBoxResult.Yes)
                    _relatorioService.ImprimirFechamentoCaixa(caixaFechada, operador, resultado, movsFechamento);
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
            caixaPanelAberto.Visibility   = Visibility.Collapsed;
            caixaPanelFechado.Visibility  = Visibility.Collapsed;
            caixaPanelHistorico.Visibility = Visibility.Visible;
            CaixaHistoricoCarregar();
        }

        private void CaixaHistoricoCarregar() {
            try {
                var sessoes = _caixaService.ListarHistorico().ToList();
                caixaDgHistorico.ItemsSource = sessoes;
                caixaHistoricoTxtTotal.Text  = $"{sessoes.Count} {(sessoes.Count == 1 ? "sessão" : "sessões")}";
                caixaHistoricoMovPanel.Visibility = Visibility.Collapsed;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar histórico:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CaixaDgHistorico_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (caixaDgHistorico.SelectedItem is not ViewModels.CaixaSessaoViewModel sessao) {
                caixaHistoricoMovPanel.Visibility = Visibility.Collapsed;
                return;
            }
            try {
                var movs = _caixaService.ListarMovimentacoes(sessao.Id)
                    .OrderByDescending(m => m.Data)
                    .Select(m => new ViewModels.MovimentacaoViewModel {
                        Id             = m.Id,
                        Tipo           = m.Tipo,
                        Descricao      = m.Descricao,
                        Valor          = m.Valor,
                        Data           = m.Data,
                        Origem         = m.Origem,
                        FormaPagamento = m.FormaPagamento
                    }).ToList();

                caixaHistoricoDgMovs.ItemsSource = movs;
                var fechamento = sessao.DataFechamento.HasValue
                    ? $"  →  {sessao.FechamentoTexto}  (fechado por {sessao.OperadorFechamentoTexto})"
                    : "  →  em aberto";
                caixaHistoricoMovSessaoInfo.Text =
                    $"Aberto por {sessao.NomeOperador}  ·  {sessao.AberturaTexto}{fechamento}";
                caixaHistoricoMovQtd.Text = movs.Count == 0
                    ? "sem movimentações"
                    : $"{movs.Count} {(movs.Count == 1 ? "movimentação" : "movimentações")}";
                caixaHistoricoMovPanel.Visibility = Visibility.Visible;
            } catch (Exception ex) {
                MessageBox.Show("Erro ao carregar movimentações:\n\n" + ex.Message,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVoltarCaixa_Click(object sender, RoutedEventArgs e) {
            caixaPanelHistorico.Visibility = Visibility.Collapsed;
            CaixaCarregar();
        }

        // Registra uma forma de pagamento de uma venda no caixa
        private void CaixaRegistrarVenda(decimal valor, string forma, Guid pedidoId, int numeroPedido) {
            if (_caixaAtivo == null) return;
            try {
                _caixaService.RegistrarVenda(
                    _caixaAtivo.Id,
                    $"Venda #{numeroPedido:D4}",
                    valor,
                    forma,
                    pedidoId);
            } catch (Exception ex) {
                MessageBox.Show(
                    $"Aviso: o movimento da venda #{numeroPedido:D4} ({forma}) não foi registrado no caixa.\n\n{ex.Message}",
                    "Erro no Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
