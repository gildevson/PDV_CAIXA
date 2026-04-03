using System.Globalization;
namespace PDV_CAIXA.ViewModels {
    public class PedidoResumoViewModel {
        public Guid     Id             { get; set; }
        public int      Numero         { get; set; }
        public DateTime Data           { get; set; }
        public decimal  Total          { get; set; }
        public string   Status         { get; set; } = string.Empty;
        public string   FormaPagamento { get; set; } = string.Empty;
        public string   Operador       { get; set; } = string.Empty;

        public string NumeroTexto        => $"#{Numero:D4}";
        public string TotalTexto         => Total.ToString("C2", new CultureInfo("pt-BR"));
        public string DataTexto          => Data.ToString("dd/MM/yyyy HH:mm");
        public string PagamentoTexto     => FormaPagamento switch {
            "pix"     => "PIX",
            "dinheiro"=> "Dinheiro",
            "credito" => "Crédito",
            "debito"  => "Débito",
            _         => FormaPagamento
        };
    }
}
