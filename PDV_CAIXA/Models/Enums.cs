namespace PDV_CAIXA.Models {

    /// <summary>
    /// Classifica cada lançamento no caixa.
    /// Permite filtros, relatórios e auditoria por tipo.
    /// </summary>
    public enum TipoMovimento {
        Abertura,    // saldo inicial ao abrir o caixa
        Venda,       // entrada gerada por venda finalizada
        Sangria,     // retirada de dinheiro do caixa (ex.: depósito bancário)
        Suprimento,  // adição de dinheiro ao caixa (ex.: troco para o caixa)
        Estorno,     // devolução de uma venda cancelada
        Fechamento,  // lançamento de fechamento (saldo real informado)
        Manual       // entrada ou saída avulsa informada pelo operador
    }

    /// <summary>
    /// Formas de pagamento aceitas pelo sistema.
    /// Dinheiro = caixa físico. Cartão/PIX = controle separado.
    /// </summary>
    public enum FormaPagamento {
        Dinheiro,
        Credito,
        Debito,
        Pix
    }

    // ── Helpers para converter entre string (banco) e enum ────────────────

    public static class FormaPagamentoExtensions {
        public static string ToDb(this FormaPagamento f) => f switch {
            FormaPagamento.Dinheiro => "dinheiro",
            FormaPagamento.Credito  => "credito",
            FormaPagamento.Debito   => "debito",
            FormaPagamento.Pix      => "pix",
            _                       => "dinheiro"
        };

        public static FormaPagamento FromDb(string s) => s switch {
            "credito" => FormaPagamento.Credito,
            "debito"  => FormaPagamento.Debito,
            "pix"     => FormaPagamento.Pix,
            _         => FormaPagamento.Dinheiro
        };

        public static string Label(this FormaPagamento f) => f switch {
            FormaPagamento.Dinheiro => "Dinheiro",
            FormaPagamento.Credito  => "Cartão Crédito",
            FormaPagamento.Debito   => "Cartão Débito",
            FormaPagamento.Pix      => "PIX",
            _                       => "Dinheiro"
        };
    }

    public static class TipoMovimentoExtensions {
        public static string ToDb(this TipoMovimento t) => t switch {
            TipoMovimento.Abertura   => "abertura",
            TipoMovimento.Venda      => "venda",
            TipoMovimento.Sangria    => "sangria",
            TipoMovimento.Suprimento => "suprimento",
            TipoMovimento.Estorno    => "estorno",
            TipoMovimento.Fechamento => "fechamento",
            _                        => "manual"
        };

        public static TipoMovimento FromDb(string s) => s switch {
            "abertura"   => TipoMovimento.Abertura,
            "venda"      => TipoMovimento.Venda,
            "sangria"    => TipoMovimento.Sangria,
            "suprimento" => TipoMovimento.Suprimento,
            "estorno"    => TipoMovimento.Estorno,
            "fechamento" => TipoMovimento.Fechamento,
            _            => TipoMovimento.Manual
        };
    }
}
