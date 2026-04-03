namespace PDV_CAIXA.Models {
    public class Pedido {
        public Guid     Id             { get; set; }
        public int      Numero         { get; set; }
        public DateTime Data           { get; set; } = DateTime.Now;
        public decimal  Total          { get; set; }
        public Guid     UsuarioId      { get; set; }
        public string   Status         { get; set; } = "finalizado";
        public string   FormaPagamento { get; set; } = "dinheiro";
    }
}
