namespace PDV_CAIXA.Models {

    /// <summary>
    /// Representa uma forma de pagamento usada em um pedido.
    /// Um pedido com forma_pagamento = 'misto' terá N linhas aqui,
    /// uma para cada forma utilizada (dinheiro + pix, etc.).
    /// </summary>
    public class PedidoPagamento {
        public Guid   Id       { get; set; }
        public Guid   PedidoId { get; set; }

        /// <summary>Forma de pagamento: dinheiro | credito | debito | pix</summary>
        public string Forma    { get; set; } = "dinheiro";

        /// <summary>Valor pago nessa forma.</summary>
        public decimal Valor   { get; set; }

        /// <summary>Troco (somente para dinheiro).</summary>
        public decimal Troco   { get; set; }
    }
}
