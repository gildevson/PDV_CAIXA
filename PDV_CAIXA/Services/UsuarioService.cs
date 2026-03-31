using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Services {
    public class UsuarioService {
        private readonly UsuarioRepository _repo = new();

        public IEnumerable<Usuario> ObterTodos() =>
            _repo.ObterTodos();

        // Carrega foto — usar somente ao editar ou ver perfil
        public Usuario? ObterPorId(Guid id) =>
            _repo.ObterPorId(id);

        public void Inserir(string nome, string senha, string perfil, byte[]? foto = null) {
            var hash = BCrypt.Net.BCrypt.HashPassword(senha);
            _repo.Inserir(new Usuario { Nome = nome, Senha = hash, Perfil = perfil, Foto = foto });
        }

        public void Atualizar(Guid id, string nome, string perfil, byte[]? foto = null) =>
            _repo.Atualizar(new Usuario { Id = id, Nome = nome, Perfil = perfil, Foto = foto });

        public void AlterarSenha(Guid id, string novaSenha) =>
            _repo.AlterarSenha(id, BCrypt.Net.BCrypt.HashPassword(novaSenha));

        public void Excluir(Guid id) =>
            _repo.Excluir(id);
    }
}
