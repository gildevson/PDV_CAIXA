using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class ProdutoRepository {
        private readonly Conexao _conexao = new();

        public IEnumerable<Produto> ObterTodos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<Produto>(
                "SELECT id, nome, descricao, codigo_barras, preco, estoque, ativo, foto FROM produtos ORDER BY nome");
        }

        public Produto? ObterPorId(Guid id) {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Produto>(
                "SELECT id, nome, descricao, codigo_barras, preco, estoque, ativo, foto FROM produtos WHERE id = @Id",
                new { Id = id });
        }

        public Produto? ObterPorCodigoBarras(string codigo) {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Produto>(
                "SELECT id, nome, descricao, codigo_barras, preco, estoque, ativo FROM produtos WHERE codigo_barras = @Codigo AND ativo = true",
                new { Codigo = codigo });
        }

        public void Inserir(Produto produto) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"INSERT INTO produtos (nome, descricao, codigo_barras, preco, estoque, ativo, foto)
                  VALUES (@Nome, @Descricao, @CodigoBarras, @Preco, @Estoque, @Ativo, @Foto)",
                produto);
        }

        public void Atualizar(Produto produto) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"UPDATE produtos
                  SET nome = @Nome, descricao = @Descricao, codigo_barras = @CodigoBarras,
                      preco = @Preco, estoque = @Estoque, ativo = @Ativo, foto = @Foto
                  WHERE id = @Id",
                produto);
        }

        public void Excluir(Guid id) {
            using var conn = _conexao.CriarConexao();
            conn.Execute("DELETE FROM produtos WHERE id = @Id", new { Id = id });
        }
    }
}
