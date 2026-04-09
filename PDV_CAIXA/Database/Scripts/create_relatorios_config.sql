-- ============================================================
-- BANCO: pdv_wpf
-- TABELA: relatorios_config
-- Criado: 2026-04-09
-- ============================================================

-- ── 1. CRIAR TABELA ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS relatorios_config (
    id            UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    nome          VARCHAR(100)  NOT NULL,
    descricao     TEXT          NULL,
    tipo          VARCHAR(50)   NOT NULL,
    nome_arquivo  VARCHAR(100)  NOT NULL,
    ordem         INTEGER       NOT NULL DEFAULT 0,
    ativo         BOOLEAN       NOT NULL DEFAULT true
);

-- ── 2. ÍNDICES ───────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_relatorios_config_ativo ON relatorios_config(ativo);
CREATE INDEX IF NOT EXISTS idx_relatorios_config_ordem ON relatorios_config(ordem);

-- ── 3. INSERTS PADRÃO ────────────────────────────────────────
INSERT INTO relatorios_config (nome, descricao, tipo, nome_arquivo, ordem) VALUES
    ('Produtos Cadastrados', 'Lista todos os produtos com preço, estoque e status', 'Produtos', 'RelatorioProdutos.rdlc', 1),
    ('Usuários Cadastrados', 'Lista todos os usuários e seus perfis de acesso',     'Usuarios', 'RelatorioUsuarios.rdlc', 2)
ON CONFLICT DO NOTHING;

-- ── 4. CONSULTAS ÚTEIS ───────────────────────────────────────

-- Listar relatórios ativos ordenados
SELECT id, nome, descricao, tipo, ordem FROM relatorios_config WHERE ativo = true ORDER BY ordem;
