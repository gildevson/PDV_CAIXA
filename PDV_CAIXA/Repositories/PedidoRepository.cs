using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Repositories {
    public class PedidoRepository {
        private readonly Conexao _conexao = new();

        private const string SqlResumo =
            @"SELECT p.id, p.numero, p.data, p.total, p.status, p.forma_pagamento, u.nome AS operador
              FROM pedidos p
              JOIN usuarios u ON u.id = p.usuario_id";

        public IEnumerable<PedidoResumoViewModel> ListarRecentes(int limit = 30) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<PedidoResumoViewModel>(
                SqlResumo + " ORDER BY p.data DESC LIMIT @Limit",
                new { Limit = limit });
        }

        public IEnumerable<PedidoResumoViewModel> ListarPorPeriodo(DateTime? inicio = null) {
            using var conn = _conexao.CriarConexao();
            var where = inicio.HasValue ? " WHERE p.data >= @Inicio" : "";
            return conn.Query<PedidoResumoViewModel>(
                SqlResumo + where + " ORDER BY p.data DESC",
                inicio.HasValue ? (object)new { Inicio = inicio.Value } : null);
        }

        public (int Quantidade, decimal Faturamento) ObterTotais(DateTime? inicio = null) {
            using var conn = _conexao.CriarConexao();
            var where = inicio.HasValue
                ? " WHERE data >= @Inicio AND status = 'finalizado'"
                : " WHERE status = 'finalizado'";
            var r = conn.QueryFirst(
                "SELECT COUNT(*)::int AS qtd, COALESCE(SUM(total),0)::numeric AS fat FROM pedidos" + where,
                inicio.HasValue ? (object)new { Inicio = inicio.Value } : null);
            return ((int)r.qtd, (decimal)r.fat);
        }

        public IEnumerable<PedidoResumoViewModel> BuscarPorNumero(int numero) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<PedidoResumoViewModel>(
                SqlResumo + " WHERE p.numero = @Numero",
                new { Numero = numero });
        }

        public IEnumerable<PedidoResumoViewModel> BuscarPorId(Guid id) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<PedidoResumoViewModel>(
                SqlResumo + " WHERE p.id = @Id",
                new { Id = id });
        }

        public IEnumerable<PedidoItem> ObterItens(Guid pedidoId) {
            using var conn = _conexao.CriarConexao();
            return conn.Query<PedidoItem>(
                @"SELECT id, pedido_id, produto_id, nome_produto, quantidade, preco_unitario, subtotal
                  FROM pedido_itens
                  WHERE pedido_id = @PedidoId
                  ORDER BY nome_produto",
                new { PedidoId = pedidoId });
        }

        public void Salvar(Pedido pedido, IEnumerable<PedidoItem> itens) {
            using var conn = _conexao.CriarConexao();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // Salva o pedido e recupera o número gerado pelo SERIAL
            pedido.Numero = conn.ExecuteScalar<int>(
                @"INSERT INTO pedidos (id, data, total, usuario_id, status, forma_pagamento)
                  VALUES (@Id, @Data, @Total, @UsuarioId, @Status, @FormaPagamento)
                  RETURNING numero",
                pedido, tx);

            // Salva cada item e desconta estoque
            foreach (var item in itens) {
                conn.Execute(
                    @"INSERT INTO pedido_itens (id, pedido_id, produto_id, nome_produto, quantidade, preco_unitario, subtotal)
                      VALUES (@Id, @PedidoId, @ProdutoId, @NomeProduto, @Quantidade, @PrecoUnitario, @Subtotal)",
                    item, tx);

                conn.Execute(
                    "UPDATE produtos SET estoque = estoque - @Qty WHERE id = @ProdutoId",
                    new { Qty = item.Quantidade, item.ProdutoId }, tx);
            }

            tx.Commit();
        }
    }
}
