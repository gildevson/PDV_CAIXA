-- ============================================================
-- BANCO: pdv_wpf
-- TABELA: relatorios_config
-- ALTERAÇÃO: adiciona coluna nome_arquivo
-- Criado: 2026-04-09
-- ============================================================

-- ── 1. ALTERAR TABELA ────────────────────────────────────────
ALTER TABLE relatorios_config
    ADD COLUMN IF NOT EXISTS nome_arquivo VARCHAR(100) NOT NULL DEFAULT '';

-- ── 2. ATUALIZAR REGISTROS EXISTENTES ────────────────────────
UPDATE relatorios_config SET nome_arquivo = 'RelatorioProdutos.rdlc' WHERE tipo = 'Produtos' AND nome_arquivo = '';
UPDATE relatorios_config SET nome_arquivo = 'RelatorioUsuarios.rdlc' WHERE tipo = 'Usuarios' AND nome_arquivo = '';

-- ── 3. CONSULTAS ÚTEIS ───────────────────────────────────────

-- Verificar resultado
SELECT id, nome, tipo, nome_arquivo, ativo FROM relatorios_config ORDER BY ordem;
