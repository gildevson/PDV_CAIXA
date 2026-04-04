namespace PDV_CAIXA.Models {

    /// <summary>
    /// Representa uma sessão de caixa — do momento da abertura até o fechamento.
    /// </summary>
    public class Caixa {
        public Guid      Id             { get; set; }
        public DateTime  DataAbertura   { get; set; }
        public DateTime? DataFechamento { get; set; }
        public decimal   SaldoInicial   { get; set; }
        public string    Status         { get; set; } = "aberto"; // aberto | fechado
        public Guid      UsuarioId      { get; set; }

        // ── Campos preenchidos no fechamento ──────────────────────────────

        /// <summary>Total de entradas em dinheiro (vendas + suprimentos - sangrias).</summary>
        public decimal? TotalDinheiro { get; set; }

        /// <summary>Total de vendas pagas em cartão de crédito.</summary>
        public decimal? TotalCredito  { get; set; }

        /// <summary>Total de vendas pagas em cartão de débito.</summary>
        public decimal? TotalDebito   { get; set; }

        /// <summary>Total de vendas pagas via PIX.</summary>
        public decimal? TotalPix      { get; set; }

        /// <summary>
        /// Saldo esperado no físico = SaldoInicial + entradas(dinheiro) - saídas(dinheiro).
        /// Calculado automaticamente no fechamento.
        /// </summary>
        public decimal? SaldoEsperado { get; set; }

        /// <summary>Valor contado fisicamente pelo operador no momento do fechamento.</summary>
        public decimal? SaldoReal     { get; set; }

        /// <summary>
        /// Diferença = SaldoReal - SaldoEsperado.
        /// Positivo = sobra. Negativo = falta.
        /// </summary>
        public decimal? Diferenca     { get; set; }

        /// <summary>Observação opcional registrada pelo operador no fechamento.</summary>
        public string?  Observacao    { get; set; }
    }
}
