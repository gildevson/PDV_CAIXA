-- ============================================================
-- BANCO: pdv_wpf
-- TABELA: usuarios
-- Atualizado: 2026-03-31
-- ============================================================

-- ── 1. CRIAR TABELA ──────────────────────────────────────────
CREATE TABLE usuarios (
    id     UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    nome   VARCHAR(100) NOT NULL UNIQUE,
    senha  VARCHAR(255) NOT NULL,
    perfil VARCHAR(20)  NOT NULL DEFAULT 'usuario'
               CHECK (perfil IN ('admin', 'usuario')),
    foto   BYTEA        NULL
);

-- ── 2. ADICIONAR COLUNA FOTO (tabela já existente) ───────────
ALTER TABLE usuarios ADD COLUMN foto BYTEA NULL;

-- ── 3. INSERT DE EXEMPLO ─────────────────────────────────────
-- ATENÇÃO: a senha deve ser gerada pela aplicação (BCrypt).
-- Use a tela de cadastro para criar usuários com hash correto.
--
-- Exemplo de insert direto (senha em texto puro — NÃO use em produção):
--
-- INSERT INTO usuarios (nome, senha, perfil)
-- VALUES ('admin', 'sua_senha_aqui', 'admin');
--
-- Exemplo com hash BCrypt já gerado externamente:
-- (hash abaixo equivale à senha '1234')
-- Senha: 1234 (hash BCrypt gerado pela aplicação)
INSERT INTO usuarios (nome, senha, perfil)
VALUES (
    'admin',
    '$2a$11$NpryVgx0YdyHOrReESQXU.VkJS/b5xFISl0CEc/.rdSB.hKziNXy.',
    'admin'
)
ON CONFLICT (nome) DO NOTHING;

-- ── 4. CONSULTAS ÚTEIS ───────────────────────────────────────

-- Listar todos os usuários (sem foto)
SELECT id, nome, perfil FROM usuarios ORDER BY nome;

-- Buscar usuário por nome
SELECT id, nome, perfil FROM usuarios WHERE nome = 'admin';

-- Verificar se usuário tem foto
SELECT id, nome,
       CASE WHEN foto IS NULL THEN 'Sem foto' ELSE 'Com foto' END AS situacao_foto
FROM usuarios;

-- Remover foto de um usuário
-- UPDATE usuarios SET foto = NULL WHERE nome = 'admin';

-- Excluir usuário
-- DELETE FROM usuarios WHERE nome = 'admin';
