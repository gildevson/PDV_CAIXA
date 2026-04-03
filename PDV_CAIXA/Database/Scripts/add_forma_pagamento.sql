-- ============================================================
-- MIGRATION: Adiciona forma de pagamento à tabela pedidos
-- Execute uma única vez no banco de dados
-- ============================================================

ALTER TABLE pedidos
    ADD COLUMN IF NOT EXISTS forma_pagamento VARCHAR(20) NOT NULL DEFAULT 'dinheiro';

ALTER TABLE pedidos DROP CONSTRAINT IF EXISTS chk_forma_pagamento;
ALTER TABLE pedidos
    ADD CONSTRAINT chk_forma_pagamento CHECK (forma_pagamento IN ('pix', 'dinheiro', 'credito', 'debito'));
