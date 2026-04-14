-- Tabela de Contas Contábeis
CREATE TABLE IF NOT EXISTS contas_contabeis (
    id                            SERIAL PRIMARY KEY,
    codigo_contabil               VARCHAR(30) NOT NULL,
    codigo_reduzido               VARCHAR(30),
    descricao                     VARCHAR(50) NOT NULL,
    grupo                         VARCHAR(100),
    tipo                          VARCHAR(100),
    codigo_historico              VARCHAR(30),
    historico                     VARCHAR(150),
    grupo_contabil_entrada        VARCHAR(100),
    grupo_contabil_saida          VARCHAR(100),
    centro_de_custo               VARCHAR(100),
    exibir_em_lancamentos_manuais BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em                     TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_contas_contabeis_codigo
    ON contas_contabeis (codigo_contabil);
