using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class RelatorioConfigRepository {
        private readonly Conexao _conexao = new();

        public IEnumerable<RelatorioConfig> ObterTodos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<RelatorioConfig>(
                "SELECT id, nome, descricao, tipo, nome_arquivo, ordem, ativo FROM relatorios_config ORDER BY ordem, nome");
        }

        public IEnumerable<RelatorioConfig> ObterAtivos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<RelatorioConfig>(
                "SELECT id, nome, descricao, tipo, nome_arquivo, ordem, ativo FROM relatorios_config WHERE ativo = true ORDER BY ordem, nome");
        }

        public void Inserir(RelatorioConfig config) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"INSERT INTO relatorios_config (nome, descricao, tipo, nome_arquivo, ordem, ativo)
                  VALUES (@Nome, @Descricao, @Tipo, @NomeArquivo, @Ordem, @Ativo)",
                config);
        }

        public void Atualizar(RelatorioConfig config) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"UPDATE relatorios_config
                  SET nome = @Nome, descricao = @Descricao, tipo = @Tipo,
                      nome_arquivo = @NomeArquivo, ordem = @Ordem, ativo = @Ativo
                  WHERE id = @Id",
                config);
        }

        public void Excluir(Guid id) {
            using var conn = _conexao.CriarConexao();
            conn.Execute("DELETE FROM relatorios_config WHERE id = @Id", new { Id = id });
        }
    }
}
