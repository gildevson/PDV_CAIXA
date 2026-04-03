-- ============================================================
-- MIGRATION: Adiciona número sequencial à tabela pedidos
-- Execute uma única vez no banco de dados
-- ============================================================

ALTER TABLE pedidos ADD COLUMN IF NOT EXISTS numero SERIAL;

CREATE UNIQUE INDEX IF NOT EXISTS idx_pedidos_numero ON pedidos(numero);

