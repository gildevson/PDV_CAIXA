using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class UsuarioRepository {
        private readonly Conexao _conexao = new();

        public IEnumerable<Usuario> ObterTodos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<Usuario>(
                "SELECT id, nome, perfil, foto FROM usuarios ORDER BY nome");
        }

        // Com foto — usada ao editar ou ver perfil
        public Usuario? ObterPorId(Guid id) {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Usuario>(
                "SELECT id, nome, senha, perfil, foto FROM usuarios WHERE id = @Id",
                new { Id = id });
        }

        // Com foto — usada no login
        public Usuario? ObterPorNome(string nome) {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Usuario>(
                "SELECT id, nome, senha, perfil FROM usuarios WHERE nome = @Nome",
                new { Nome = nome });
        }

        public void Inserir(Usuario usuario) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                "INSERT INTO usuarios (nome, senha, perfil, foto) VALUES (@Nome, @Senha, @Perfil, @Foto)",
                usuario);
        }

        public void Atualizar(Usuario usuario) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                "UPDATE usuarios SET nome = @Nome, perfil = @Perfil, foto = @Foto WHERE id = @Id",
                usuario);
        }

        public void AlterarSenha(Guid id, string senhaHash) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                "UPDATE usuarios SET senha = @Senha WHERE id = @Id",
                new { Senha = senhaHash, Id = id });
        }

        public void Excluir(Guid id) {
            using var conn = _conexao.CriarConexao();
            conn.Execute("DELETE FROM usuarios WHERE id = @Id", new { Id = id });
        }
    }
}
