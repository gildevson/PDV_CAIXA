using System.Globalization;

namespace PDV_CAIXA.ViewModels {
    public class CaixaSessaoViewModel {
        private static readonly CultureInfo PtBR = CultureInfo.GetCultureInfo("pt-BR");

        public Guid      Id             { get; set; }
        public DateTime  DataAbertura   { get; set; }
        public DateTime? DataFechamento { get; set; }
        public decimal   SaldoInicial   { get; set; }
        public decimal   TotalEntradas  { get; set; }
        public decimal   TotalSaidas    { get; set; }
        public decimal   SaldoFinal     { get; set; }
        public string    Status         { get; set; } = "";
        public string    NomeOperador   { get; set; } = "";

        // ── Campos do fechamento (null quando sessão ainda aberta) ────────
        public decimal? TotalDinheiro  { get; set; }
        public decimal? TotalCredito   { get; set; }
        public decimal? TotalDebito    { get; set; }
        public decimal? TotalPix       { get; set; }
        public decimal? SaldoEsperado  { get; set; }
        public decimal? SaldoReal      { get; set; }
        public decimal? Diferenca      { get; set; }

        // ── Textos formatados ─────────────────────────────────────────────
        public string AberturaTexto     => DataAbertura.ToString("dd/MM/yyyy  HH:mm");
        public string FechamentoTexto   => DataFechamento?.ToString("dd/MM/yyyy  HH:mm") ?? "Em aberto";
        public string SaldoInicialTexto => SaldoInicial.ToString("C2", PtBR);
        public string EntradasTexto     => TotalEntradas.ToString("C2", PtBR);
        public string SaidasTexto       => TotalSaidas.ToString("C2", PtBR);
        public string SaldoFinalTexto   => SaldoFinal.ToString("C2", PtBR);
        public string DinheiroTexto     => (TotalDinheiro ?? 0).ToString("C2", PtBR);
        public string CreditoTexto      => (TotalCredito  ?? 0).ToString("C2", PtBR);
        public string DebitoTexto       => (TotalDebito   ?? 0).ToString("C2", PtBR);
        public string PixtTexto         => (TotalPix      ?? 0).ToString("C2", PtBR);
        public string DiferencaTexto    => Diferenca.HasValue
            ? (Diferenca >= 0 ? "+ " : "− ") + Math.Abs(Diferenca.Value).ToString("C2", PtBR)
            : "-";

        public string DuracaoTexto {
            get {
                var fim = DataFechamento ?? DateTime.Now;
                var dur = fim - DataAbertura;
                if (dur.TotalHours >= 1) return $"{(int)dur.TotalHours}h {dur.Minutes:D2}min";
                return $"{dur.Minutes}min";
            }
        }
    }
}
