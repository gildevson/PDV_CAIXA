-- ══════════════════════════════════════════════════════════════════════
-- VIEW: vw_produtos_ativos
-- Retorna apenas os produtos com ativo = true, usada no relatório RDLC
-- ══════════════════════════════════════════════════════════════════════

-- ── 1. Criar/Substituir a VIEW ─────────────────────────────────────────
CREATE OR REPLACE VIEW vw_produtos_ativos AS
SELECT
    id,
    nome,
    COALESCE(codigo_barras, '—') AS codigo_barras,
    preco,
    desconto,
    estoque
FROM produtos
WHERE ativo = true
ORDER BY nome;

-- ── 2. Inserir registro na tabela de relatórios ───────────────────────
INSERT INTO relatorios_config (id, nome, descricao, tipo, nome_arquivo, ordem, ativo)
VALUES (
    gen_random_uuid(),
    'Produtos Ativos',
    'Relatório de produtos cadastrados e ativos no sistema',
    'ProdutosAtivos',
    'RelatorioProdutosAtivos.rdlc',
    3,
    true
)
ON CONFLICT DO NOTHING;
