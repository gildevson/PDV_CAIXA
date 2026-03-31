using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Services {
    public class AuthService {
        private readonly UsuarioRepository _repo = new();

        public Task<Usuario?> ValidateCredentialsAsync(string nome, string senha) {
            var usuario = _repo.ObterPorNome(nome);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(senha, usuario.Senha))
                return Task.FromResult<Usuario?>(null);

            return Task.FromResult<Usuario?>(usuario);
        }
    }
}
