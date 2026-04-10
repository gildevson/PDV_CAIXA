-- ============================================================
-- VIEW: vw_pedidos
-- Visão consolidada dos pedidos para manutenção e consultas
-- ============================================================

CREATE OR REPLACE VIEW vw_pedidos AS
SELECT
    p.numero,
    p.data                                      AS data_hora,
    TO_CHAR(p.data AT TIME ZONE 'America/Sao_Paulo', 'DD/MM/YYYY HH24:MI')
                                                AS data_fmt,
    p.total,
    p.status,
    p.forma_pagamento,
    u.nome                                      AS operador,
    COUNT(pi.id)                                AS qtd_itens,
    SUM(pi.quantidade)                          AS qtd_produtos,
    p.id                                        AS pedido_id,
    p.usuario_id
FROM pedidos p
JOIN usuarios u          ON u.id  = p.usuario_id
LEFT JOIN pedido_itens pi ON pi.pedido_id = p.id
GROUP BY p.id, p.numero, p.data, p.total, p.status, p.forma_pagamento, u.nome, p.usuario_id
ORDER BY p.data DESC;

-- ============================================================
-- VIEW: vw_pedido_itens
-- Todos os itens de todos os pedidos (para consulta detalhada)
-- ============================================================

CREATE OR REPLACE VIEW vw_pedido_itens AS
SELECT
    p.numero                                    AS pedido_numero,
    TO_CHAR(p.data AT TIME ZONE 'America/Sao_Paulo', 'DD/MM/YYYY HH24:MI')
                                                AS data_fmt,
    p.status,
    p.forma_pagamento,
    u.nome                                      AS operador,
    pi.nome_produto,
    pi.quantidade,
    pi.preco_unitario,
    pi.subtotal,
    p.total                                     AS total_pedido,
    pi.id                                       AS item_id,
    pi.pedido_id,
    pi.produto_id
FROM pedido_itens pi
JOIN pedidos  p ON p.id  = pi.pedido_id
JOIN usuarios u ON u.id  = p.usuario_id
ORDER BY p.data DESC, p.numero, pi.nome_produto;

-- ============================================================
-- VIEW: vw_pedido_pagamentos
-- Breakdown de pagamentos por pedido (pagamentos mistos)
-- ============================================================

CREATE OR REPLACE VIEW vw_pedido_pagamentos AS
SELECT
    p.numero                                    AS pedido_numero,
    TO_CHAR(p.data AT TIME ZONE 'America/Sao_Paulo', 'DD/MM/YYYY HH24:MI')
                                                AS data_fmt,
    u.nome                                      AS operador,
    p.total                                     AS total_pedido,
    pp.forma,
    pp.valor,
    pp.troco,
    pp.id                                       AS pagamento_id,
    pp.pedido_id
FROM pedido_pagamentos pp
JOIN pedidos  p ON p.id = pp.pedido_id
JOIN usuarios u ON u.id = p.usuario_id
ORDER BY p.data DESC, p.numero, pp.forma;

-- ============================================================
-- EXEMPLOS DE USO
-- ============================================================

-- Resumo de vendas do dia:
-- SELECT * FROM vw_pedidos
-- WHERE data_hora::date = CURRENT_DATE;

-- Total vendido por operador (mês atual):
-- SELECT operador, COUNT(*) AS pedidos, SUM(total) AS total_vendido
-- FROM vw_pedidos
-- WHERE DATE_TRUNC('month', data_hora) = DATE_TRUNC('month', CURRENT_DATE)
-- GROUP BY operador ORDER BY total_vendido DESC;

-- Pedidos cancelados:
-- SELECT * FROM vw_pedidos WHERE status = 'cancelado';

-- Itens de um pedido específico:
-- SELECT * FROM vw_pedido_itens WHERE pedido_numero = 42;

-- Produtos mais vendidos (quantidade):
-- SELECT nome_produto, SUM(quantidade) AS total_qtd, SUM(subtotal) AS total_valor
-- FROM vw_pedido_itens
-- WHERE status = 'finalizado'
-- GROUP BY nome_produto ORDER BY total_qtd DESC;

-- Vendas por forma de pagamento:
-- SELECT forma_pagamento, COUNT(*) AS pedidos, SUM(total) AS total
-- FROM vw_pedidos WHERE status = 'finalizado'
-- GROUP BY forma_pagamento ORDER BY total DESC;

-- Detalhamento de pagamentos mistos:
-- SELECT * FROM vw_pedido_pagamentos WHERE pedido_numero = 10;
