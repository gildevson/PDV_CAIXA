using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class CaixaRepository {
        private readonly Conexao _conexao = new();

        public Caixa? ObterAberto() {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Caixa>(
                "SELECT * FROM caixa WHERE status = 'aberto' ORDER BY data_abertura DESC LIMIT 1");
        }

        public Caixa Abrir(decimal saldoInicial, Guid usuarioId) {
            using var conn = _conexao.CriarConexao();
            var id = conn.ExecuteScalar<Guid>(
                @"INSERT INTO caixa (saldo_inicial, usuario_id)
                  VALUES (@SaldoInicial, @UsuarioId)
                  RETURNING id",
                new { SaldoInicial = saldoInicial, UsuarioId = usuarioId });
            return conn.QueryFirst<Caixa>("SELECT * FROM caixa WHERE id = @Id", new { Id = id });
        }

        public void Fechar(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                "UPDATE caixa SET status = 'fechado', data_fechamento = now() WHERE id = @Id",
                new { Id = caixaId });
        }

        public void InserirMovimentacao(MovimentacaoCaixa mov) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"INSERT INTO movimentacao_caixa (caixa_id, tipo, descricao, valor, origem, pedido_id)
                  VALUES (@CaixaId, @Tipo, @Descricao, @Valor, @Origem, @PedidoId)",
                mov);
        }

        public IEnumerable<MovimentacaoCaixa> ListarMovimentacoes(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<MovimentacaoCaixa>(
                "SELECT * FROM movimentacao_caixa WHERE caixa_id = @CaixaId ORDER BY data",
                new { CaixaId = caixaId });
        }

        public IEnumerable<Caixa> ListarHistorico() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<Caixa>(
                "SELECT * FROM caixa ORDER BY data_abertura DESC LIMIT 50");
        }

        public IEnumerable<(Caixa caixa, string nomeOperador)> ListarHistoricoDetalhado() {
            using var conn = _conexao.CriarConexao();
            var rows = conn.Query(
                @"SELECT c.id, c.data_abertura, c.data_fechamento, c.saldo_inicial,
                         c.status, c.usuario_id,
                         COALESCE(u.nome, 'Desconhecido') AS nome_operador
                  FROM caixa c
                  LEFT JOIN usuarios u ON u.id = c.usuario_id
                  ORDER BY c.data_abertura DESC
                  LIMIT 100");

            return rows.Select(r => (
                new Caixa {
                    Id             = r.id,
                    DataAbertura   = r.data_abertura,
                    DataFechamento = r.data_fechamento,
                    SaldoInicial   = r.saldo_inicial,
                    Status         = r.status,
                    UsuarioId      = r.usuario_id
                },
                (string)r.nome_operador
            )).ToList();
        }

        public (decimal entradas, decimal saidas) ObterTotais(Guid caixaId) {
            using var conn = _conexao.CriarConexao();
            var entradas = conn.ExecuteScalar<decimal>(
                "SELECT COALESCE(SUM(valor), 0) FROM movimentacao_caixa WHERE caixa_id = @Id AND tipo = 'entrada'",
                new { Id = caixaId });
            var saidas = conn.ExecuteScalar<decimal>(
                "SELECT COALESCE(SUM(valor), 0) FROM movimentacao_caixa WHERE caixa_id = @Id AND tipo = 'saida'",
                new { Id = caixaId });
            return (entradas, saidas);
        }
    }
}
