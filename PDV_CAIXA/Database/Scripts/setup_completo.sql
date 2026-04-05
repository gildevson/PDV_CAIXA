-- ============================================================
-- PDV CAIXA — SETUP COMPLETO DO BANCO
-- Execute conectado ao banco pdv_wpf
-- Seguro para rodar em banco existente (usa IF NOT EXISTS)
-- Atualizado: 2026-04-04
-- ============================================================

-- ══════════════════════════════════════════════════════════════
-- 1. USUÁRIOS
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS usuarios (
    id     UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    nome   VARCHAR(100) NOT NULL UNIQUE,
    senha  VARCHAR(255) NOT NULL,
    perfil VARCHAR(20)  NOT NULL DEFAULT 'usuario'
               CHECK (perfil IN ('admin', 'usuario')),
    foto   BYTEA        NULL
);

-- Usuário admin padrão (senha: 1234 — altere após o primeiro acesso)
INSERT INTO usuarios (nome, senha, perfil)
VALUES (
    'admin',
    '$2a$11$NpryVgx0YdyHOrReESQXU.VkJS/b5xFISl0CEc/.rdSB.hKziNXy.',
    'admin'
)
ON CONFLICT (nome) DO NOTHING;

-- ══════════════════════════════════════════════════════════════
-- 2. PRODUTOS
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS produtos (
    id             UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    nome           VARCHAR(100)  NOT NULL,
    descricao      TEXT          NULL,
    codigo_barras  VARCHAR(50)   NULL UNIQUE,
    preco          NUMERIC(10,2) NOT NULL DEFAULT 0,
    estoque        INTEGER       NOT NULL DEFAULT 0,
    ativo          BOOLEAN       NOT NULL DEFAULT true,
    foto           BYTEA         NULL,
    desconto       NUMERIC(5,2)  NOT NULL DEFAULT 0
                       CHECK (desconto >= 0 AND desconto <= 100)
);

CREATE INDEX IF NOT EXISTS idx_produtos_nome          ON produtos(nome);
CREATE INDEX IF NOT EXISTS idx_produtos_codigo_barras ON produtos(codigo_barras);
CREATE INDEX IF NOT EXISTS idx_produtos_ativo         ON produtos(ativo);

-- ══════════════════════════════════════════════════════════════
-- 3. PEDIDOS
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS pedidos (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    numero          SERIAL        NOT NULL UNIQUE,
    data            TIMESTAMPTZ   NOT NULL DEFAULT now(),
    total           NUMERIC(10,2) NOT NULL DEFAULT 0,
    usuario_id      UUID          NOT NULL REFERENCES usuarios(id),
    status          VARCHAR(20)   NOT NULL DEFAULT 'finalizado'
                    CHECK (status IN ('finalizado', 'cancelado')),
    forma_pagamento VARCHAR(20)   NOT NULL DEFAULT 'dinheiro'
                    CHECK (forma_pagamento IN ('pix', 'dinheiro', 'credito', 'debito', 'misto'))
);

CREATE TABLE IF NOT EXISTS pedido_itens (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id       UUID          NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    produto_id      UUID          NOT NULL REFERENCES produtos(id),
    nome_produto    VARCHAR(100)  NOT NULL,
    quantidade      INTEGER       NOT NULL CHECK (quantidade > 0),
    preco_unitario  NUMERIC(10,2) NOT NULL,
    subtotal        NUMERIC(10,2) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_pedidos_numero      ON pedidos(numero);
CREATE INDEX IF NOT EXISTS idx_pedidos_data        ON pedidos(data DESC);
CREATE INDEX IF NOT EXISTS idx_pedidos_usuario     ON pedidos(usuario_id);
CREATE INDEX IF NOT EXISTS idx_pedido_itens_pedido ON pedido_itens(pedido_id);

-- ══════════════════════════════════════════════════════════════
-- 4. CAIXA
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS caixa (
    id                    UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    data_abertura         TIMESTAMPTZ   NOT NULL DEFAULT now(),
    data_fechamento       TIMESTAMPTZ   NULL,
    saldo_inicial         NUMERIC(10,2) NOT NULL DEFAULT 0,
    status                VARCHAR(20)   NOT NULL DEFAULT 'aberto'
                              CHECK (status IN ('aberto', 'fechado')),
    usuario_id            UUID          NOT NULL REFERENCES usuarios(id),
    usuario_fechamento_id UUID          NULL     REFERENCES usuarios(id),
    -- Campos preenchidos no fechamento
    total_dinheiro  NUMERIC(10,2) NULL,
    total_credito   NUMERIC(10,2) NULL,
    total_debito    NUMERIC(10,2) NULL,
    total_pix       NUMERIC(10,2) NULL,
    saldo_esperado  NUMERIC(10,2) NULL,
    saldo_real      NUMERIC(10,2) NULL,
    diferenca       NUMERIC(10,2) NULL,
    observacao      VARCHAR(500)  NULL
);

CREATE INDEX IF NOT EXISTS idx_caixa_status ON caixa(status);

-- ══════════════════════════════════════════════════════════════
-- 5. MOVIMENTAÇÕES DE CAIXA
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS movimentacao_caixa (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    caixa_id        UUID          NOT NULL REFERENCES caixa(id) ON DELETE CASCADE,
    tipo            VARCHAR(10)   NOT NULL CHECK (tipo IN ('entrada', 'saida')),
    descricao       VARCHAR(200)  NOT NULL,
    valor           NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    data            TIMESTAMPTZ   NOT NULL DEFAULT now(),
    origem          VARCHAR(20)   NOT NULL DEFAULT 'manual'
                        CHECK (origem IN ('manual', 'venda')),
    pedido_id       UUID          NULL REFERENCES pedidos(id),
    tipo_movimento  VARCHAR(20)   NOT NULL DEFAULT 'manual'
                        CHECK (tipo_movimento IN ('abertura','venda','sangria','suprimento','estorno','fechamento','manual')),
    forma_pagamento VARCHAR(20)   NULL
                        CHECK (forma_pagamento IN ('dinheiro','credito','debito','pix'))
);

CREATE INDEX IF NOT EXISTS idx_movimentacao_caixa_id  ON movimentacao_caixa(caixa_id);
CREATE INDEX IF NOT EXISTS idx_movimentacao_tipo_mov  ON movimentacao_caixa(caixa_id, tipo_movimento);
CREATE INDEX IF NOT EXISTS idx_movimentacao_forma_pag ON movimentacao_caixa(caixa_id, forma_pagamento);

-- ══════════════════════════════════════════════════════════════
-- 6. PAGAMENTOS POR PEDIDO (misto)
-- ══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS pedido_pagamentos (
    id        UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id UUID          NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    forma     VARCHAR(20)   NOT NULL
                  CHECK (forma IN ('dinheiro','credito','debito','pix')),
    valor     NUMERIC(10,2) NOT NULL CHECK (valor > 0),
    troco     NUMERIC(10,2) NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_pedido_pagamentos_pedido ON pedido_pagamentos(pedido_id);

-- ══════════════════════════════════════════════════════════════
-- 7. MIGRAÇÕES PARA BANCOS EXISTENTES
--    (sem efeito em banco recém-criado pelo setup_completo)
-- ══════════════════════════════════════════════════════════════

-- Adiciona desconto em produtos (caso tabela já existia sem a coluna)
ALTER TABLE produtos ADD COLUMN IF NOT EXISTS desconto NUMERIC(5,2) NOT NULL DEFAULT 0;
ALTER TABLE produtos DROP CONSTRAINT IF EXISTS chk_desconto;
ALTER TABLE produtos ADD CONSTRAINT chk_desconto CHECK (desconto >= 0 AND desconto <= 100);

-- Adiciona numero em pedidos (caso tabela já existia sem a coluna)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name='pedidos' AND column_name='numero'
    ) THEN
        ALTER TABLE pedidos ADD COLUMN numero SERIAL;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_pedidos_numero ON pedidos(numero);
    END IF;
END $$;

-- Adiciona forma_pagamento em pedidos (caso tabela já existia sem a coluna)
ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS forma_pagamento VARCHAR(20) NOT NULL DEFAULT 'dinheiro';
ALTER TABLE pedidos DROP CONSTRAINT IF EXISTS chk_forma_pagamento;
ALTER TABLE pedidos ADD CONSTRAINT chk_forma_pagamento
    CHECK (forma_pagamento IN ('pix', 'dinheiro', 'credito', 'debito', 'misto'));

-- Adiciona usuario_fechamento_id em caixa (caso tabela já existia sem a coluna)
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS usuario_fechamento_id UUID NULL REFERENCES usuarios(id);

-- Adiciona colunas de fechamento em caixa (caso tabela já existia sem elas)
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS total_dinheiro NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS total_credito  NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS total_debito   NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS total_pix      NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS saldo_esperado NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS saldo_real     NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS diferenca      NUMERIC(10,2) NULL;
ALTER TABLE caixa ADD COLUMN IF NOT EXISTS observacao     VARCHAR(500)  NULL;

-- Adiciona colunas de rastreamento em movimentacao_caixa (caso já existia sem elas)
ALTER TABLE movimentacao_caixa ADD COLUMN IF NOT EXISTS forma_pagamento VARCHAR(20) NULL;
ALTER TABLE movimentacao_caixa DROP CONSTRAINT IF EXISTS movimentacao_caixa_forma_pagamento_check;
ALTER TABLE movimentacao_caixa ADD CONSTRAINT movimentacao_caixa_forma_pagamento_check
    CHECK (forma_pagamento IN ('dinheiro','credito','debito','pix'));

ALTER TABLE movimentacao_caixa ADD COLUMN IF NOT EXISTS tipo_movimento VARCHAR(20) NOT NULL DEFAULT 'manual';
ALTER TABLE movimentacao_caixa DROP CONSTRAINT IF EXISTS movimentacao_caixa_tipo_movimento_check;
ALTER TABLE movimentacao_caixa ADD CONSTRAINT movimentacao_caixa_tipo_movimento_check
    CHECK (tipo_movimento IN ('abertura','venda','sangria','suprimento','estorno','fechamento','manual'));

-- ══════════════════════════════════════════════════════════════
-- 8. VIEW DE RESUMO POR FORMA DE PAGAMENTO
-- ══════════════════════════════════════════════════════════════
CREATE OR REPLACE VIEW vw_caixa_formas AS
SELECT
    mc.caixa_id,
    mc.forma_pagamento,
    SUM(CASE WHEN mc.tipo = 'entrada' THEN mc.valor ELSE 0 END)   AS total_entradas,
    SUM(CASE WHEN mc.tipo = 'saida'   THEN mc.valor ELSE 0 END)   AS total_saidas,
    SUM(CASE WHEN mc.tipo = 'entrada' THEN mc.valor ELSE -mc.valor END) AS saldo_liquido
FROM movimentacao_caixa mc
WHERE mc.forma_pagamento IS NOT NULL
GROUP BY mc.caixa_id, mc.forma_pagamento;

-- ══════════════════════════════════════════════════════════════
SELECT 'Setup completo executado com sucesso!' AS status;
-- ══════════════════════════════════════════════════════════════
