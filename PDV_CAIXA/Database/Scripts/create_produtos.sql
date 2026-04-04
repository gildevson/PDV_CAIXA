-- ============================================================
-- BANCO: pdv_wpf
-- TABELA: produtos
-- Atualizado: 2026-03-31
-- ============================================================

-- ── 1. CRIAR TABELA ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS produtos (
    id             UUID           PRIMARY KEY DEFAULT gen_random_uuid(),
    nome           VARCHAR(100)   NOT NULL,
    descricao      TEXT           NULL,
    codigo_barras  VARCHAR(50)    NULL UNIQUE,
    preco          NUMERIC(10,2)  NOT NULL DEFAULT 0,
    estoque        INTEGER        NOT NULL DEFAULT 0,
    ativo          BOOLEAN        NOT NULL DEFAULT true,
    foto           BYTEA          NULL
);

-- ── 2. ÍNDICES ───────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_produtos_nome          ON produtos(nome);
CREATE INDEX IF NOT EXISTS idx_produtos_codigo_barras ON produtos(codigo_barras);
CREATE INDEX IF NOT EXISTS idx_produtos_ativo         ON produtos(ativo);

-- ── 3. INSERTS DE EXEMPLO ────────────────────────────────────
INSERT INTO produtos (nome, descricao, codigo_barras, preco, estoque) VALUES
    ('Coca-Cola 350ml',  'Refrigerante lata',        '7891000315507', 4.50,  100),
    ('Água Mineral 500ml', 'Água sem gás',            '7896006701600', 2.00,  200),
    ('Pão Francês',      'Unidade',                  NULL,            0.75,  150)
ON CONFLICT DO NOTHING;

-- ── 4. CONSULTAS ÚTEIS ───────────────────────────────────────

-- Listar produtos ativos
SELECT id, nome, codigo_barras, preco, estoque FROM produtos WHERE ativo = true ORDER BY nome;

-- Buscar por código de barras
SELECT id, nome, preco, estoque FROM produtos WHERE codigo_barras = '7891000315507';

-- Buscar por nome (parcial)
SELECT id, nome, preco, estoque FROM produtos WHERE nome ILIKE '%coca%';
