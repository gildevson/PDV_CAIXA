namespace PDV_CAIXA.Models {

    /// <summary>
    /// Cada lançamento (entrada ou saída) dentro de uma sessão de caixa.
    /// </summary>
    public class MovimentacaoCaixa {
        public Guid     Id             { get; set; }
        public Guid     CaixaId        { get; set; }

        /// <summary>Direção do lançamento: entrada | saida</summary>
        public string   Tipo           { get; set; } = "";

        /// <summary>Descrição livre do lançamento.</summary>
        public string   Descricao      { get; set; } = "";

        public decimal  Valor          { get; set; }
        public DateTime Data           { get; set; }

        /// <summary>Origem: manual | venda</summary>
        public string   Origem         { get; set; } = "manual";

        /// <summary>
        /// Classificação detalhada do lançamento.
        /// Valores: abertura | venda | sangria | suprimento | estorno | fechamento | manual
        /// </summary>
        public string   TipoMovimento  { get; set; } = "manual";

        /// <summary>
        /// Forma de pagamento associada (null quando não se aplica, ex.: sangria).
        /// Valores: dinheiro | credito | debito | pix
        /// </summary>
        public string?  FormaPagamento { get; set; }

        /// <summary>Pedido de origem (quando Origem = "venda").</summary>
        public Guid?    PedidoId       { get; set; }
    }
}
