# PDV Caixa

Sistema de **Ponto de Venda (PDV)** desktop desenvolvido em **C# com WPF (.NET 8)** e banco de dados **PostgreSQL**. Projeto de estudo com foco em arquitetura em camadas, autenticação segura e interface moderna com tema escuro.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma |
| WPF | — | Interface gráfica (Windows) |
| PostgreSQL | 13+ | Banco de dados |
| Dapper | 2.1.72 | ORM leve para consultas SQL |
| Npgsql | 10.0.2 | Driver PostgreSQL para .NET |
| BCrypt.Net-Next | 4.1.0 | Hash de senhas |

---

## Arquitetura

O projeto segue arquitetura em **camadas separadas**, facilitando manutenção e evolução:

```
PDV_CAIXA/
├── Config/              # Configurações (leitura do appsettings.json)
├── Converters/          # Conversores WPF (ex: byte[] → BitmapImage)
├── Data/                # Conexão com o banco de dados
├── Database/
│   ├── Docs/            # Documentação das telas
│   └── Scripts/         # Scripts SQL (CREATE, INSERT)
├── Models/              # Entidades do domínio
├── Repositories/        # Acesso a dados (SQL via Dapper)
├── Services/            # Regras de negócio
├── ViewModels/          # Modelos para binding com a UI
└── Views/               # Telas XAML
```

### Regra de responsabilidade por camada

| Camada | Responsabilidade |
|---|---|
| `Models/` | Somente propriedades, sem lógica |
| `Repositories/` | Somente SQL — sem regras de negócio |
| `Services/` | Regras de negócio, hash de senha, validações |
| `ViewModels/` | Dados moldados para exibição na tela |
| `Views/` | Interface — chama `Service`, nunca `Repository` diretamente |

---

## Funcionalidades

- Login com autenticação segura (senha em **BCrypt hash**)
- Menu lateral responsivo com **foto de perfil circular**
- Foto de perfil armazenada em banco como `BYTEA`
- Controle de acesso por **perfil** (`admin` / `usuario`)
- Gerenciamento de usuários (CRUD completo) — visível somente para `admin`
- Proteção para não excluir o próprio usuário logado
- ID do usuário em **UUID** gerado pelo PostgreSQL
- Interface com tema escuro moderno

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 13+](https://www.postgresql.org/download/)
- Windows 10 ou superior (WPF)

---

## Configuração do Banco de Dados

### 1. Criar o banco

```sql
CREATE DATABASE pdv_wpf;
```

### 2. Criar a tabela de usuários

Conectado ao banco `pdv_wpf`, execute:

```sql
CREATE TABLE usuarios (
    id     UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    nome   VARCHAR(100) NOT NULL UNIQUE,
    senha  VARCHAR(255) NOT NULL,
    perfil VARCHAR(20)  NOT NULL DEFAULT 'usuario'
               CHECK (perfil IN ('admin', 'usuario')),
    foto   BYTEA        NULL
);
```

### 3. Inserir o usuário admin padrão

> Senha: `1234` — hash gerado pelo BCrypt da aplicação

```sql
INSERT INTO usuarios (nome, senha, perfil)
VALUES (
    'admin',
    '$2a$11$NpryVgx0YdyHOrReESQXU.VkJS/b5xFISl0CEc/.rdSB.hKziNXy.',
    'admin'
)
ON CONFLICT (nome) DO NOTHING;
```

---

## Configuração da Aplicação

Edite o arquivo `PDV_CAIXA/appsettings.json` com suas credenciais do PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pdv_wpf;Username=postgres;Password=SUA_SENHA"
  }
}
```

---

## Como Rodar

```bash
# Clonar o repositório
git clone https://github.com/seu-usuario/PDV_CAIXA.git
cd PDV_CAIXA

# Restaurar dependências e compilar
dotnet build PDV_CAIXA/PDV_CAIXA.csproj

# Executar
dotnet run --project PDV_CAIXA/PDV_CAIXA.csproj
```

Ou abra a solution `PDV_CAIXA.sln` no **Visual Studio 2022** e pressione `F5`.

---

## Fluxo da Aplicação

```
Iniciar app
    │
    ▼
LoginWindow
    │
    ├── Credenciais inválidas → mensagem de erro
    │
    └── Login OK
            │
            ▼
        MainWindow (menu lateral)
            │
            ├── 🏠 Início         → tela de boas-vindas
            │
            └── 👥 Usuários       → somente perfil admin
                    │
                    ├── + Novo Usuário  → CadastroUsuarioWindow
                    ├── Editar          → CadastroUsuarioWindow (modo edição)
                    └── Excluir         → confirmação (bloqueado para si mesmo)
```

---

## Segurança

- Senhas armazenadas com **BCrypt** (custo 11) — nunca em texto puro
- Verificação de senha via `BCrypt.Verify()` — sem comparar strings diretamente
- Queries parametrizadas via Dapper — protegido contra **SQL Injection**
- Connection string fora do código-fonte (`appsettings.json`)
- Controle de acesso por perfil na UI e nas operações

---

## Estrutura de Telas

### Login
- Layout responsivo em duas colunas (painel lateral + formulário)
- Logo **RME** no painel esquerdo
- Validação de campos com mensagem de erro inline

### Menu Principal
- Sidebar com foto de perfil circular do usuário logado
- Iniciais como fallback quando não há foto cadastrada
- Badge de perfil (Administrador / Usuário)
- Navegação por páginas sem abrir novas janelas

### Cadastro de Usuário
- Upload de foto com preview circular em tempo real
- Redimensionamento automático para 300×300px (JPEG 85%)
- Seleção de perfil via cards clicáveis
- Validação inline por campo
- Modo criação e modo edição

---

## Pacotes NuGet

```xml
<PackageReference Include="BCrypt.Net-Next" Version="4.1.0" />
<PackageReference Include="Dapper"          Version="2.1.72" />
<PackageReference Include="Npgsql"          Version="10.0.2" />
```

---

## Contribuindo

1. Faça um fork do projeto
2. Crie uma branch: `git checkout -b feature/minha-feature`
3. Commit: `git commit -m 'feat: adiciona minha feature'`
4. Push: `git push origin feature/minha-feature`
5. Abra um Pull Request

---

## Licença

Este projeto é de uso educacional e livre para estudos.
