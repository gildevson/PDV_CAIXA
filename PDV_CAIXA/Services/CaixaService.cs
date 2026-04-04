using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Services {

    /// <summary>
    /// Regras de negócio do módulo de caixa.
    /// Toda operação passa por aqui — o Repository só faz I/O.
    /// </summary>
    public class CaixaService {
        private readonly CaixaRepository _repo = new();

        // ════════════════════════════════════════════════════════════
        // SESSÃO
        // ════════════════════════════════════════════════════════════

        /// <summary>Retorna o caixa aberto no momento, ou null.</summary>
        public Caixa? ObterCaixaAberto() => _repo.ObterAberto();

        /// <summary>
        /// Abre uma nova sessão de caixa.
        /// Regra: não pode abrir se já existe um caixa aberto.
        /// </summary>
        public Caixa AbrirCaixa(decimal saldoInicial, Guid usuarioId) {
            if (_repo.ObterAberto() != null)
                throw new InvalidOperationException(
                    "Já existe um caixa aberto. Feche-o antes de abrir um novo.");

            if (saldoInicial < 0)
                throw new ArgumentException("Saldo inicial não pode ser negativo.");

            return _repo.Abrir(saldoInicial, usuarioId);
        }

        /// <summary>
        /// Fecha a sessão de caixa calculando todos os totais automaticamente.
        /// </summary>
        /// <param name="caixaId">ID da sessão a fechar.</param>
        /// <param name="saldoRealDinheiro">Valor físico contado pelo operador.</param>
        /// <param name="observacao">Observação opcional (ex.: explicação de diferença).</param>
        public ResultadoFechamento FecharCaixa(Guid caixaId, decimal saldoRealDinheiro,
                                               string? observacao = null) {
            var caixa = _repo.ObterAberto()
                ?? throw new InvalidOperationException("Nenhum caixa aberto encontrado.");

            if (caixa.Id != caixaId)
                throw new InvalidOperationException("ID de caixa inválido.");

            if (caixa.Status == "fechado")
                throw new InvalidOperationException("Este caixa já está fechado.");

            // Totais por forma de pagamento
            var formas = _repo.ObterTotaisPorForma(caixaId);

            // Saldo esperado = inicial + tudo que entrou/saiu em dinheiro
            var saldoEsperado = caixa.SaldoInicial + formas.Dinheiro;
            var diferenca     = saldoRealDinheiro - saldoEsperado;

            _repo.Fechar(
                caixaId,
                totalDinheiro : formas.Dinheiro,
                totalCredito  : formas.Credito,
                totalDebito   : formas.Debito,
                totalPix      : formas.Pix,
                saldoEsperado : saldoEsperado,
                saldoReal     : saldoRealDinheiro,
                diferenca     : diferenca,
                observacao    : observacao);

            return new ResultadoFechamento {
                SaldoInicial   = caixa.SaldoInicial,
                TotalDinheiro  = formas.Dinheiro,
                TotalCredito   = formas.Credito,
                TotalDebito    = formas.Debito,
                TotalPix       = formas.Pix,
                TotalGeral     = formas.Total,
                SaldoEsperado  = saldoEsperado,
                SaldoReal      = saldoRealDinheiro,
                Diferenca      = diferenca
            };
        }

        // ════════════════════════════════════════════════════════════
        // MOVIMENTAÇÕES
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra uma entrada de dinheiro gerada por uma venda.
        /// Chamado automaticamente ao finalizar um pedido pago em dinheiro.
        /// </summary>
        public void RegistrarVenda(Guid caixaId, string descricao, decimal valor,
                                   string formaPagamento, Guid pedidoId) {
            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId        = caixaId,
                Tipo           = "entrada",
                Descricao      = descricao,
                Valor          = valor,
                Origem         = "venda",
                TipoMovimento  = "venda",
                FormaPagamento = formaPagamento,
                PedidoId       = pedidoId
            });
        }

        /// <summary>
        /// Sangria: retirada de dinheiro físico do caixa.
        /// Ex.: depósito bancário durante o turno.
        /// </summary>
        public void RegistrarSangria(Guid caixaId, string descricao, decimal valor) {
            if (valor <= 0) throw new ArgumentException("Valor da sangria deve ser maior que zero.");

            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId        = caixaId,
                Tipo           = "saida",
                Descricao      = descricao,
                Valor          = valor,
                Origem         = "manual",
                TipoMovimento  = "sangria",
                FormaPagamento = "dinheiro"  // sangria é sempre dinheiro físico
            });
        }

        /// <summary>
        /// Suprimento: adição de dinheiro ao caixa físico.
        /// Ex.: adicionar troco no início do turno.
        /// </summary>
        public void RegistrarSuprimento(Guid caixaId, string descricao, decimal valor) {
            if (valor <= 0) throw new ArgumentException("Valor do suprimento deve ser maior que zero.");

            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId        = caixaId,
                Tipo           = "entrada",
                Descricao      = descricao,
                Valor          = valor,
                Origem         = "manual",
                TipoMovimento  = "suprimento",
                FormaPagamento = "dinheiro"
            });
        }

        /// <summary>
        /// Entrada manual avulsa (compatibilidade com botão "Nova Entrada" existente).
        /// </summary>
        public void RegistrarEntrada(Guid caixaId, string descricao, decimal valor,
                                     Guid? pedidoId = null, string origem = "manual") {
            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId       = caixaId,
                Tipo          = "entrada",
                Descricao     = descricao,
                Valor         = valor,
                Origem        = origem,
                TipoMovimento = pedidoId.HasValue ? "venda" : "manual",
                PedidoId      = pedidoId
            });
        }

        /// <summary>
        /// Saída manual avulsa (compatibilidade com botão "Nova Saída" existente).
        /// </summary>
        public void RegistrarSaida(Guid caixaId, string descricao, decimal valor) {
            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId       = caixaId,
                Tipo          = "saida",
                Descricao     = descricao,
                Valor         = valor,
                Origem        = "manual",
                TipoMovimento = "manual"
            });
        }

        // ════════════════════════════════════════════════════════════
        // CONSULTAS
        // ════════════════════════════════════════════════════════════

        public IEnumerable<MovimentacaoCaixa> ListarMovimentacoes(Guid caixaId)
            => _repo.ListarMovimentacoes(caixaId);

        /// <summary>
        /// Resumo rápido para o header da tela de caixa aberto.
        /// </summary>
        public (decimal saldoAtual, decimal entradas, decimal saidas) ObterResumo(Guid caixaId) {
            var (entradas, saidas) = _repo.ObterTotais(caixaId);
            var caixa = _repo.ObterAberto();
            var saldoInicial = caixa?.SaldoInicial ?? 0;
            return (saldoInicial + entradas - saidas, entradas, saidas);
        }

        /// <summary>Totais por forma de pagamento para exibir na tela de fechamento.</summary>
        public TotaisPorForma ObterTotaisPorForma(Guid caixaId)
            => _repo.ObterTotaisPorForma(caixaId);

        /// <summary>Histórico com nome do operador.</summary>
        public IEnumerable<CaixaSessaoViewModel> ListarHistorico() {
            return _repo.ListarHistoricoDetalhado().Select(t => {
                var (entradas, saidas) = _repo.ObterTotais(t.caixa.Id);
                return new CaixaSessaoViewModel {
                    Id             = t.caixa.Id,
                    DataAbertura   = t.caixa.DataAbertura,
                    DataFechamento = t.caixa.DataFechamento,
                    SaldoInicial   = t.caixa.SaldoInicial,
                    TotalEntradas  = entradas,
                    TotalSaidas    = saidas,
                    SaldoFinal     = t.caixa.SaldoInicial + entradas - saidas,
                    Status         = t.caixa.Status,
                    NomeOperador   = t.nomeOperador,
                    // Campos do fechamento (null quando aberto)
                    TotalDinheiro  = t.caixa.TotalDinheiro,
                    TotalCredito   = t.caixa.TotalCredito,
                    TotalDebito    = t.caixa.TotalDebito,
                    TotalPix       = t.caixa.TotalPix,
                    SaldoEsperado  = t.caixa.SaldoEsperado,
                    SaldoReal      = t.caixa.SaldoReal,
                    Diferenca      = t.caixa.Diferenca
                };
            });
        }

        // ── Guard interno ─────────────────────────────────────────────────
        private void GuardarCaixaAberto(Guid caixaId) {
            var aberto = _repo.ObterAberto();
            if (aberto == null || aberto.Id != caixaId)
                throw new InvalidOperationException(
                    "Operação negada: não há caixa aberto com este ID.");
        }
    }

    // ════════════════════════════════════════════════════════════
    // DTO de retorno do fechamento
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Resultado calculado pelo FecharCaixa — exibido na tela de fechamento.
    /// </summary>
    public class ResultadoFechamento {
        public decimal SaldoInicial  { get; set; }
        public decimal TotalDinheiro { get; set; }
        public decimal TotalCredito  { get; set; }
        public decimal TotalDebito   { get; set; }
        public decimal TotalPix      { get; set; }
        public decimal TotalGeral    { get; set; }
        public decimal SaldoEsperado { get; set; }
        public decimal SaldoReal     { get; set; }
        public decimal Diferenca     { get; set; }

        /// <summary>true = sobra, false = falta, null = zerado.</summary>
        public bool? Sobra => Diferenca == 0 ? null : Diferenca > 0;
    }
}
