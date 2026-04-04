namespace PDV_CAIXA.Models {
    public class Caixa {
        public Guid      Id              { get; set; }
        public DateTime  DataAbertura    { get; set; }
        public DateTime? DataFechamento  { get; set; }
        public decimal   SaldoInicial    { get; set; }
        public string    Status          { get; set; } = "aberto";
        public Guid      UsuarioId       { get; set; }
    }
}
