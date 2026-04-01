-- ============================================================
-- BANCO: pdv_wpf
-- TABELAS: pedidos, pedido_itens
-- Atualizado: 2026-03-31
-- ============================================================

-- ── 1. PEDIDOS (cabeçalho) ───────────────────────────────────
CREATE TABLE pedidos (
    id          UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    data        TIMESTAMPTZ   NOT NULL DEFAULT now(),
    total       NUMERIC(10,2) NOT NULL DEFAULT 0,
    usuario_id  UUID          NOT NULL REFERENCES usuarios(id),
    status      VARCHAR(20)   NOT NULL DEFAULT 'finalizado'
                CHECK (status IN ('finalizado', 'cancelado'))
);

-- ── 2. ITENS DO PEDIDO ───────────────────────────────────────
CREATE TABLE pedido_itens (
    id              UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id       UUID          NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    produto_id      UUID          NOT NULL REFERENCES produtos(id),
    nome_produto    VARCHAR(100)  NOT NULL,
    quantidade      INTEGER       NOT NULL CHECK (quantidade > 0),
    preco_unitario  NUMERIC(10,2) NOT NULL,
    subtotal        NUMERIC(10,2) NOT NULL
);

-- ── 3. ÍNDICES ───────────────────────────────────────────────
CREATE INDEX idx_pedidos_data       ON pedidos(data DESC);
CREATE INDEX idx_pedidos_usuario    ON pedidos(usuario_id);
CREATE INDEX idx_pedido_itens_pedido ON pedido_itens(pedido_id);

-- ── 4. CONSULTAS ÚTEIS ───────────────────────────────────────

-- Listar pedidos com totais
SELECT p.id, p.data, p.total, p.status, u.nome AS operador
FROM pedidos p
JOIN usuarios u ON u.id = p.usuario_id
ORDER BY p.data DESC;

-- Itens de um pedido
SELECT pi.nome_produto, pi.quantidade, pi.preco_unitario, pi.subtotal
FROM pedido_itens pi
WHERE pi.pedido_id = '<pedido_id>';
