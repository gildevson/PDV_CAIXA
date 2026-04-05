using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class CaixaRepository {
        private readonly Conexao _conexao = new();

        // ════════════════════════════════════════════════════════════
        // SESSÃO DE CAIXA
        // ════════════════════════════════════════════════════════════

        /// <summary>Retorna o caixa aberto no momento, ou null se nenhum.</summary>
        public Caixa? ObterAberto() {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Caixa>(
                "SELECT * FROM caixa WHERE status = 'aberto' ORDER BY data_abertura DESC LIMIT 1");
        }

        /// <summary>Abre uma nova sessão de caixa com saldo inicial.</summary>
        public Caixa Abrir(decimal saldoInicial, Guid usuarioId) {
            using var conn = _conexao.CriarConexao();
            var id = conn.ExecuteScalar<Guid>(
                @"INSERT INTO caixa (saldo_inicial, usuario_id)
                  VALUES (@SaldoInicial, @UsuarioId)
                  RETURNING id",
                new { SaldoInicial = saldoInicial, UsuarioId = usuarioId });
            return conn.QueryFirst<Caixa>("SELECT * FROM caixa WHERE id = @Id", new { Id = id });
        }

        /// <summary>
        /// Fecha a sessão salvando todos os totais calculados e a diferença de caixa.
        /// </summary>
        public void Fechar(Guid caixaId, decimal totalDinheiro, decimal totalCredito,
                           decimal totalDebito, decimal totalPix,
                           decimal saldoEsperado, decimal saldoReal, decimal diferenca,
                           string? observacao, Guid? usuarioFechamentoId = null) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"UPDATE caixa SET
                    status                = 'fechado',
                    data_fechamento       = now(),
                    total_dinheiro        = @TotalDinheiro,
                    total_credito         = @TotalCredito,
                    total_debito          = @TotalDebito,
                    total_pix             = @TotalPix,
                    saldo_esperado        = @SaldoEsperado,
                    saldo_real            = @SaldoReal,
                    diferenca             = @Diferenca,
                    observacao            = @Observacao,
                    usuario_fechamento_id = @UsuarioFechamentoId
                  WHERE id = @Id",
                new { Id = caixaId, TotalDinheiro = totalDinheiro, TotalCredito = totalCredito,
                      TotalDebito = totalDebito, TotalPix = totalPix,
                      SaldoEsperado = saldoEsperado, SaldoReal = saldoReal,
                      Diferenca = diferenca, Observacao = observacao,
                      UsuarioFechamentoId = usuarioFechamentoId });
        }

        // ════════════════════════════════════════════════════════════
        // MOVIMENTAÇÕES
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Insere um lançamento na sessão de caixa.
        /// Usa as colunas originais (sempre funciona) e tenta enriquecer
        /// com tipo_movimento/forma_pagamento se a migration já foi executada.
        /// </summary>
        public void InserirMovimentacao(MovimentacaoCaixa mov) {
            using var conn = _conexao.CriarConexao();

            // INSERT com colunas que SEMPRE existem — nunca falha
            var id = conn.ExecuteScalar<Guid>(
                @"INSERT INTO movimentacao_caixa
                    (caixa_id, tipo, descricao, valor, origem, pedido_id)
                  VALUES
                    (@CaixaId, @Tipo, @Descricao, @Valor, @Origem, @PedidoId)
                  RETURNING id",
                mov);

            // UPDATE com colunas novas — só funciona após rodar migration_caixa_completo.sql
            try {
                conn.Execute(
                    @"UPDATE movimentacao_caixa
                      SET tipo_movimento  = @TipoMovimento,
                          forma_pagamento = @FormaPagamento
                      WHERE id = @Id",
                    new { mov.TipoMovimento, mov.FormaPagamento, Id = id });
            } catch {
                // Colunas ainda não existem — migration pendente. Movimento salvo sem elas.
            }
        }

        /// <summary>Lista todas as movimentações de uma sessão ordenadas por data.</summary>
        public IEnumerable<MovimentacaoCaixa> ListarMovimentacoes(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<MovimentacaoCaixa>(
                "SELECT * FROM movimentacao_caixa WHERE caixa_id = @CaixaId ORDER BY data",
                new { CaixaId = caixaId });
        }

        // ════════════════════════════════════════════════════════════
        // TOTAIS / AGREGAÇÕES
        // ════════════════════════════════════════════════════════════

        /// <summary>Totais simples de entradas e saídas (qualquer forma de pagamento).</summary>
        public (decimal entradas, decimal saidas) ObterTotais(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            var entradas = conn.ExecuteScalar<decimal>(
                "SELECT COALESCE(SUM(valor),0) FROM movimentacao_caixa WHERE caixa_id=@Id AND tipo='entrada'",
                new { Id = caixaId });
            var saidas = conn.ExecuteScalar<decimal>(
                "SELECT COALESCE(SUM(valor),0) FROM movimentacao_caixa WHERE caixa_id=@Id AND tipo='saida'",
                new { Id = caixaId });
            return (entradas, saidas);
        }

        /// <summary>
        /// Totais líquidos por forma de pagamento na sessão.
        /// Só conta lançamentos com forma_pagamento preenchida (vendas, suprimentos, sangrias).
        /// </summary>
        public TotaisPorForma ObterTotaisPorForma(Guid caixaId) {
            using var conn = _conexao.CriarConexao();

            // Uma query só — agrupa por forma e calcula líquido (entrada - saída)
            var rows = conn.Query(
                @"SELECT
                    forma_pagamento,
                    SUM(CASE WHEN tipo='entrada' THEN valor ELSE 0 END) AS total_entrada,
                    SUM(CASE WHEN tipo='saida'   THEN valor ELSE 0 END) AS total_saida
                  FROM movimentacao_caixa
                  WHERE caixa_id = @Id AND forma_pagamento IS NOT NULL
                  GROUP BY forma_pagamento",
                new { Id = caixaId });

            var result = new TotaisPorForma();
            foreach (var r in rows) {
                decimal liquido = (decimal)r.total_entrada - (decimal)r.total_saida;
                switch ((string)r.forma_pagamento) {
                    case "dinheiro": result.Dinheiro = liquido; break;
                    case "credito":  result.Credito  = liquido; break;
                    case "debito":   result.Debito   = liquido; break;
                    case "pix":      result.Pix      = liquido; break;
                }
            }
            return result;
        }

        /// <summary>
        /// Totais de sangrias e suprimentos separados (para auditoria).
        /// </summary>
        public (decimal sangrias, decimal suprimentos) ObterSangriaSuprimento(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            var sangrias = conn.ExecuteScalar<decimal>(
                @"SELECT COALESCE(SUM(valor),0) FROM movimentacao_caixa
                  WHERE caixa_id=@Id AND tipo_movimento='sangria'",
                new { Id = caixaId });
            var suprimentos = conn.ExecuteScalar<decimal>(
                @"SELECT COALESCE(SUM(valor),0) FROM movimentacao_caixa
                  WHERE caixa_id=@Id AND tipo_movimento='suprimento'",
                new { Id = caixaId });
            return (sangrias, suprimentos);
        }

        // ════════════════════════════════════════════════════════════
        // HISTÓRICO
        // ════════════════════════════════════════════════════════════

        /// <summary>Lista as últimas 50 sessões (compatibilidade com código existente).</summary>
        public IEnumerable<Caixa> ListarHistorico() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<Caixa>(
                "SELECT * FROM caixa ORDER BY data_abertura DESC LIMIT 50");
        }

        /// <summary>Lista as últimas 100 sessões com nomes do operador de abertura e fechamento.</summary>
        public IEnumerable<(Caixa caixa, string nomeOperador, string? nomeOperadorFechamento)> ListarHistoricoDetalhado() {
            using var conn = _conexao.CriarConexao();
            var rows = conn.Query(
                @"SELECT c.id, c.data_abertura, c.data_fechamento, c.saldo_inicial,
                         c.status, c.usuario_id, c.usuario_fechamento_id,
                         c.total_dinheiro, c.total_credito, c.total_debito, c.total_pix,
                         c.saldo_esperado, c.saldo_real, c.diferenca, c.observacao,
                         COALESCE(ua.nome, 'Desconhecido') AS nome_operador,
                         uf.nome                           AS nome_operador_fechamento
                  FROM caixa c
                  LEFT JOIN usuarios ua ON ua.id = c.usuario_id
                  LEFT JOIN usuarios uf ON uf.id = c.usuario_fechamento_id
                  ORDER BY c.data_abertura DESC
                  LIMIT 100");

            return rows.Select(r => (
                new Caixa {
                    Id                   = r.id,
                    DataAbertura         = r.data_abertura,
                    DataFechamento       = r.data_fechamento,
                    SaldoInicial         = r.saldo_inicial,
                    Status               = r.status,
                    UsuarioId            = r.usuario_id,
                    UsuarioFechamentoId  = r.usuario_fechamento_id,
                    TotalDinheiro        = r.total_dinheiro,
                    TotalCredito         = r.total_credito,
                    TotalDebito          = r.total_debito,
                    TotalPix             = r.total_pix,
                    SaldoEsperado        = r.saldo_esperado,
                    SaldoReal            = r.saldo_real,
                    Diferenca            = r.diferenca,
                    Observacao           = r.observacao
                },
                (string)r.nome_operador,
                (string?)r.nome_operador_fechamento
            )).ToList();
        }
    }

    // ════════════════════════════════════════════════════════════
    // DTO auxiliar retornado por ObterTotaisPorForma
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Totais líquidos (entrada - saída) por forma de pagamento em uma sessão.
    /// </summary>
    public class TotaisPorForma {
        public decimal Dinheiro { get; set; }
        public decimal Credito  { get; set; }
        public decimal Debito   { get; set; }
        public decimal Pix      { get; set; }
        public decimal Total    => Dinheiro + Credito + Debito + Pix;
    }
}
