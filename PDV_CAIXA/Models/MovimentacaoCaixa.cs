namespace PDV_CAIXA.Models {
    public class MovimentacaoCaixa {
        public Guid      Id        { get; set; }
        public Guid      CaixaId   { get; set; }
        public string    Tipo      { get; set; } = "";   // entrada | saida
        public string    Descricao { get; set; } = "";
        public decimal   Valor     { get; set; }
        public DateTime  Data      { get; set; }
        public string    Origem    { get; set; } = "manual"; // manual | venda
        public Guid?     PedidoId  { get; set; }
    }
}
