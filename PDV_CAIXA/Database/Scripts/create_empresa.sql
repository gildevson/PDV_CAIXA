-- ══════════════════════════════════════════════════════════════════════
-- TABELA: empresa
-- Armazena os dados da empresa que opera o PDV (linha única)
-- ══════════════════════════════════════════════════════════════════════

-- ── 1. Criar tabela ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS empresa (
    id              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    razao_social    VARCHAR(200) NOT NULL DEFAULT '',
    nome_fantasia   VARCHAR(200) NOT NULL DEFAULT '',
    cnpj            VARCHAR(18)  NOT NULL DEFAULT '',
    inscricao_est   VARCHAR(30)  NOT NULL DEFAULT '',
    telefone        VARCHAR(20)  NOT NULL DEFAULT '',
    email           VARCHAR(150) NOT NULL DEFAULT '',
    website         VARCHAR(200) NOT NULL DEFAULT '',
    cep             VARCHAR(9)   NOT NULL DEFAULT '',
    logradouro      VARCHAR(200) NOT NULL DEFAULT '',
    numero          VARCHAR(20)  NOT NULL DEFAULT '',
    complemento     VARCHAR(100) NOT NULL DEFAULT '',
    bairro          VARCHAR(100) NOT NULL DEFAULT '',
    cidade          VARCHAR(100) NOT NULL DEFAULT '',
    uf              CHAR(2)      NOT NULL DEFAULT ''
);

-- ── 2. Comentários ────────────────────────────────────────────────────
COMMENT ON TABLE empresa IS 'Dados cadastrais da empresa que opera o PDV (registro único)';

-- ── 3. Garantir linha padrão (insert se vazia) ─────────────────────────
INSERT INTO empresa (id, razao_social, nome_fantasia, cnpj, inscricao_est,
                     telefone, email, website,
                     cep, logradouro, numero, complemento, bairro, cidade, uf)
SELECT gen_random_uuid(), '', '', '', '', '', '', '', '', '', '', '', '', '', ''
WHERE NOT EXISTS (SELECT 1 FROM empresa);
