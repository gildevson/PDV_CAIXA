-- Tabela de sessões de caixa
CREATE TABLE IF NOT EXISTS caixa (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    data_abertura   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    data_fechamento TIMESTAMPTZ  NULL,
    saldo_inicial   NUMERIC(10,2) NOT NULL DEFAULT 0,
    status          VARCHAR(20)  NOT NULL DEFAULT 'aberto'
                        CHECK (status IN ('aberto', 'fechado')),
    usuario_id      UUID         NOT NULL REFERENCES usuarios(id)
);

-- Tabela de movimentações do caixa
CREATE TABLE IF NOT EXISTS movimentacao_caixa (
    id         UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    caixa_id   UUID          NOT NULL REFERENCES caixa(id) ON DELETE CASCADE,
    tipo       VARCHAR(10)   NOT NULL CHECK (tipo IN ('entrada', 'saida')),
    descricao  VARCHAR(200)  NOT NULL,
    valor      NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    data       TIMESTAMPTZ   NOT NULL DEFAULT now(),
    origem     VARCHAR(20)   NOT NULL DEFAULT 'manual'
                   CHECK (origem IN ('manual', 'venda')),
    pedido_id  UUID          NULL REFERENCES pedidos(id)
);
