# LoginWindow — Documentação

## Visão Geral

Tela de autenticação do sistema PDV Caixa. É a primeira janela exibida ao iniciar o
aplicativo (`StartupUri` definido em `App.xaml`). Após login bem-sucedido, abre a
`MainWindow` e fecha a si mesma.

---

## Layout Responsivo

A tela é dividida em duas colunas que se redimensionam proporcionalmente:

```
┌─────────────────────┬──────────────────────────┐
│                     │  Bem-vindo               │
│   🛒                │                          │
│   PDV               │  Usuário: [__________]   │
│   CAIXA             │  Senha:   [__________]   │
│                     │                          │
│  Sistema de PDV     │       [ Entrar ]         │
│                     │                          │
└─────────────────────┴──────────────────────────┘
   40% da largura          60% da largura
```

| Propriedade     | Valor              |
|-----------------|--------------------|
| Largura inicial | 460px              |
| Altura inicial  | 480px              |
| Largura mínima  | 320px              |
| Altura mínima   | 380px              |
| Redimensionável | Sim (`CanResize`)  |

---

## Componentes XAML

| Nome          | Tipo          | Descrição                          |
|---------------|---------------|------------------------------------|
| `txtUsuario`  | `TextBox`     | Campo de entrada do nome do usuário|
| `pwdSenha`    | `PasswordBox` | Campo de entrada da senha          |
| `btnLogin`    | `Button`      | Dispara a autenticação             |
| `txtStatus`   | `TextBlock`   | Exibe mensagens de erro ao usuário |

---

## Paleta de Cores

| Elemento              | Cor       | Hex       |
|-----------------------|-----------|-----------|
| Fundo principal       | Escuro    | `#1E1E2E` |
| Painel lateral        | Mais escuro | `#13131F`|
| Campos de input       | Cinza azulado | `#2A2A3E`|
| Borda dos campos      | Cinza     | `#44475A` |
| Borda focada          | Roxo      | `#7C83FF` |
| Botão normal          | Roxo      | `#7C83FF` |
| Botão hover           | Roxo médio| `#6970E0` |
| Botão pressionado     | Roxo escuro | `#555BC4`|
| Texto principal       | Branco suave | `#E0E0E0`|
| Texto secundário      | Cinza     | `#6C6F85` |
| Erro                  | Vermelho  | `#FF5555` |

---

## Fluxo de Autenticação

```
Usuário clica em "Entrar"
        │
        ▼
BtnLogin_Click (LoginWindow.xaml.cs)
        │
        ├─ Valida campos vazios
        │       └─ Se vazio → exibe mensagem em txtStatus
        │
        ▼
AuthService.ValidateCredentialsAsync(nome, senha)
        │
        ▼
Consulta SQL no PostgreSQL (banco: pdv_wpf)
SELECT COUNT(1) FROM usuarios WHERE nome = @nome AND senha = @senha
        │
        ├─ Retorna > 0 → abre MainWindow, fecha LoginWindow
        └─ Retorna 0   → exibe "Credenciais inválidas." em txtStatus
```

---

## Arquivos Relacionados

| Arquivo                          | Função                              |
|----------------------------------|-------------------------------------|
| `Views/LoginWindow.xaml`         | Layout e estilos da tela            |
| `Views/LoginWindow.xaml.cs`      | Lógica do botão e navegação         |
| `Services/Services.cs`           | Validação das credenciais           |
| `Data/conexao.cs`                | Conexão com o banco PostgreSQL      |
| `Database/Scripts/create_usuarios.sql` | Script de criação da tabela   |

---

## Como Adicionar Novo Usuário

Execute no pgAdmin ou psql conectado ao banco `pdv_wpf`:

```sql
INSERT INTO usuarios (nome, senha)
VALUES ('novo_usuario', 'senha123');
```
