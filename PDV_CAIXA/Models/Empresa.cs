namespace PDV_CAIXA.Models {
    public class Empresa {
        public Guid   Id           { get; set; }
        public string RazaoSocial  { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string Cnpj         { get; set; } = string.Empty;
        public string InscricaoEst { get; set; } = string.Empty;
        public string Telefone     { get; set; } = string.Empty;
        public string Email        { get; set; } = string.Empty;
        public string Website      { get; set; } = string.Empty;
        public string Cep          { get; set; } = string.Empty;
        public string Logradouro   { get; set; } = string.Empty;
        public string Numero       { get; set; } = string.Empty;
        public string Complemento  { get; set; } = string.Empty;
        public string Bairro       { get; set; } = string.Empty;
        public string Cidade       { get; set; } = string.Empty;
        public string Uf           { get; set; } = string.Empty;
    }
}
