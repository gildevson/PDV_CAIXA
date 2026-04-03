namespace PDV_CAIXA.Models {
    public class Produto {
        public Guid    Id            { get; set; }
        public string  Nome          { get; set; } = string.Empty;
        public string? Descricao     { get; set; }
        public string? CodigoBarras  { get; set; }
        public decimal Preco         { get; set; }
        public decimal Desconto      { get; set; } = 0;
        public int     Estoque       { get; set; }
        public bool    Ativo         { get; set; } = true;
        public byte[]? Foto          { get; set; }
    }
}
