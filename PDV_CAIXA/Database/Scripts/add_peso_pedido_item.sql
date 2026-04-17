ALTER TABLE pedido_itens
    ADD COLUMN IF NOT EXISTS peso NUMERIC(8,3) NULL;

COMMENT ON COLUMN pedido_itens.peso IS 'Peso em kg para produtos vendidos por peso. NULL para produtos por unidade.';
