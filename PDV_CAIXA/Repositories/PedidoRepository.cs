using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class PedidoRepository {
        private readonly Conexao _conexao = new();

        public void Salvar(Pedido pedido, IEnumerable<PedidoItem> itens) {
            using var conn = _conexao.CriarConexao();
            conn.Open();
            using var tx = conn.BeginTransaction();

            // Salva o pedido
            conn.Execute(
                @"INSERT INTO pedidos (id, data, total, usuario_id, status)
                  VALUES (@Id, @Data, @Total, @UsuarioId, @Status)",
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
