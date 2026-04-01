namespace PDV_CAIXA.Models {
    public class PedidoItem {
        public Guid    Id             { get; set; }
        public Guid    PedidoId       { get; set; }
        public Guid    ProdutoId      { get; set; }
        public string  NomeProduto    { get; set; } = string.Empty;
        public int     Quantidade     { get; set; }
        public decimal PrecoUnitario  { get; set; }
        public decimal Subtotal       { get; set; }
    }
}
