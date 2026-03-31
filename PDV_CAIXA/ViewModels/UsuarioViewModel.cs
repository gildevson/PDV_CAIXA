using PDV_CAIXA.Models;

namespace PDV_CAIXA.ViewModels {
    public class UsuarioViewModel {
        public Guid    Id            { get; set; }
        public string  Nome          { get; set; } = string.Empty;
        public string  Perfil        { get; set; } = string.Empty;
        public byte[]? Foto          { get; set; }
        public bool    IsCurrentUser { get; set; }

        public string Iniciais {
            get {
                var partes = Nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return partes.Length >= 2
                    ? $"{partes[0][0]}{partes[^1][0]}".ToUpper()
                    : Nome.Length >= 2 ? Nome[..2].ToUpper() : Nome.ToUpper();
            }
        }

        public Usuario ToUsuario() => new() { Id = Id, Nome = Nome, Perfil = Perfil };
    }
}
