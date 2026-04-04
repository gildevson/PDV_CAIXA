-- ============================================================
-- MIGRATION: Módulo de Caixa Completo
-- Execute UMA VEZ no banco pdv_wpf
-- ============================================================

-- ── 1. COLUNAS DE FECHAMENTO NA TABELA CAIXA ─────────────────
--      Registra os totais calculados no momento do fechamento.
ALTER TABLE caixa
    ADD COLUMN IF NOT EXISTS total_dinheiro  NUMERIC(10,2) NULL,
    ADD COLUMN IF NOT EXISTS total_credito   NUMERIC(10,2) NULL,
    ADD COLUMN IF NOT EXISTS total_debito    NUMERIC(10,2) NULL,
    ADD COLUMN IF NOT EXISTS total_pix       NUMERIC(10,2) NULL,
    ADD COLUMN IF NOT EXISTS saldo_esperado  NUMERIC(10,2) NULL,  -- saldo_inicial + entradas_dinheiro - saidas_dinheiro
    ADD COLUMN IF NOT EXISTS saldo_real      NUMERIC(10,2) NULL,  -- valor contado fisicamente pelo operador
    ADD COLUMN IF NOT EXISTS diferenca       NUMERIC(10,2) NULL,  -- saldo_real - saldo_esperado (+ sobra / - falta)
    ADD COLUMN IF NOT EXISTS observacao      VARCHAR(500)  NULL;

-- ── 2. COLUNAS NA TABELA MOVIMENTACAO_CAIXA ──────────────────
--      Adiciona forma de pagamento e tipo de movimento a cada lançamento.
ALTER TABLE movimentacao_caixa
    ADD COLUMN IF NOT EXISTS forma_pagamento VARCHAR(20) NULL
        CHECK (forma_pagamento IN ('dinheiro','credito','debito','pix')),
    ADD COLUMN IF NOT EXISTS tipo_movimento  VARCHAR(20) NOT NULL DEFAULT 'manual'
        CHECK (tipo_movimento IN ('abertura','venda','sangria','suprimento','estorno','fechamento','manual'));

-- ── 3. TABELA: PEDIDO_PAGAMENTOS ─────────────────────────────
--      Breakdown de formas de pagamento quando uma venda é "misto".
--      Cada linha = uma forma usada naquele pedido.
CREATE TABLE IF NOT EXISTS pedido_pagamentos (
    id          UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id   UUID          NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    forma       VARCHAR(20)   NOT NULL
                    CHECK (forma IN ('dinheiro','credito','debito','pix')),
    valor       NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    troco       NUMERIC(10,2) NOT NULL DEFAULT 0   -- só faz sentido para dinheiro
);

-- ── 4. ÍNDICES ────────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_movimentacao_caixa_id
    ON movimentacao_caixa(caixa_id);

CREATE INDEX IF NOT EXISTS idx_movimentacao_tipo_mov
    ON movimentacao_caixa(caixa_id, tipo_movimento);

CREATE INDEX IF NOT EXISTS idx_movimentacao_forma_pag
    ON movimentacao_caixa(caixa_id, forma_pagamento);

CREATE INDEX IF NOT EXISTS idx_pedido_pagamentos_pedido
    ON pedido_pagamentos(pedido_id);

CREATE INDEX IF NOT EXISTS idx_caixa_status
    ON caixa(status);

-- ── 5. VIEW: RESUMO DE CAIXA POR FORMA DE PAGAMENTO ──────────
--      Facilita relatórios sem precisar agregar no C#.
CREATE OR REPLACE VIEW vw_caixa_formas AS
SELECT
    mc.caixa_id,
    mc.forma_pagamento,
    SUM(CASE WHEN mc.tipo = 'entrada' THEN mc.valor ELSE 0 END) AS total_entradas,
    SUM(CASE WHEN mc.tipo = 'saida'   THEN mc.valor ELSE 0 END) AS total_saidas,
    SUM(CASE WHEN mc.tipo = 'entrada' THEN mc.valor ELSE -mc.valor END) AS saldo_liquido
FROM movimentacao_caixa mc
WHERE mc.forma_pagamento IS NOT NULL
GROUP BY mc.caixa_id, mc.forma_pagamento;

-- ── VERIFICAÇÃO FINAL ─────────────────────────────────────────
SELECT 'Migration executada com sucesso!' AS status;
