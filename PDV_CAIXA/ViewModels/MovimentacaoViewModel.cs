using System.Globalization;

namespace PDV_CAIXA.ViewModels {
    public class MovimentacaoViewModel {
        private static readonly CultureInfo _ptBR = new("pt-BR");

        public Guid    Id        { get; set; }
        public string  Tipo      { get; set; } = "";
        public string  Descricao { get; set; } = "";
        public decimal Valor     { get; set; }
        public DateTime Data     { get; set; }
        public string  Origem         { get; set; } = "manual";
        public string? FormaPagamento { get; set; }

        public string TipoTexto   => Tipo == "entrada" ? "Entrada" : "Saída";
        public string OrigemTexto => Origem == "venda"  ? "PDV"    : "Manual";
        public string FormaTexto  => FormaPagamento switch {
            "dinheiro" => "💵 Dinheiro",
            "credito"  => "💳 Crédito",
            "debito"   => "💳 Débito",
            "pix"      => "📱 PIX",
            _          => "—"
        };
        public string DataTexto   => Data.ToString("dd/MM HH:mm", _ptBR);

        public string ValorTexto  => Tipo == "entrada"
            ? $"+ {Valor.ToString("C2", _ptBR)}"
            : $"- {Valor.ToString("C2", _ptBR)}";

        // Cores para binding no DataGrid
        public string CorTipo  => Tipo == "entrada" ? "#50FA7B" : "#FF5555";
        public string CorValor => Tipo == "entrada" ? "#50FA7B" : "#FF6B6B";
    }
}
