-- Adiciona coluna de desconto (%) na tabela produtos
-- Execute uma única vez no banco de dados

ALTER TABLE produtos
    ADD COLUMN IF NOT EXISTS desconto NUMERIC(5,2) NOT NULL DEFAULT 0;

ALTER TABLE produtos DROP CONSTRAINT IF EXISTS chk_desconto;
ALTER TABLE produtos
    ADD CONSTRAINT chk_desconto CHECK (desconto >= 0 AND desconto <= 100);
