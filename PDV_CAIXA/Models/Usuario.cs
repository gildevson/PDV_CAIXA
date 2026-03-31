namespace PDV_CAIXA.Models {
    public class Usuario {
        public Guid    Id     { get; set; }
        public string  Nome   { get; set; } = string.Empty;
        public string  Senha  { get; set; } = string.Empty;
        public string  Perfil { get; set; } = "usuario";
        public byte[]? Foto   { get; set; }
    }
}
