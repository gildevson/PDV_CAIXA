# PDV Caixa

Sistema de **Ponto de Venda (PDV)** desktop desenvolvido em **C# com WPF (.NET 8)** e banco de dados **PostgreSQL**. Projeto de estudo com foco em arquitetura em camadas, autenticação segura e interface moderna com tema escuro.

---

## Tecnologias

| Tecnologia | Versão | Uso |
|---|---|---|
| .NET | 8.0 | Plataforma (net8.0-windows) |
| WPF | — | Interface gráfica (Windows) |
| PostgreSQL | 13+ | Banco de dados relacional |
| Dapper | 2.1.72 | ORM leve para consultas SQL |
| Npgsql | 10.0.2 | Driver PostgreSQL para .NET |
| BCrypt.Net-Next | 4.1.0 | Hash de senhas |
| FastReport.OpenSource | 2026.1.8 | Geração de relatórios e cupons (PDF) |
| FastReport.OpenSource.Export.PdfSimple | 2026.1.8 | Exportação dos relatórios para PDF |

---

## Funcionalidades

### Autenticação
- Login com usuário e senha
- Senha armazenada com **BCrypt hash** (custo 11)
- Controle de acesso por **perfil** (`admin` / `usuario`)

### PDV (Ponto de Venda)
- Busca de produto por **nome** ou **código de barras**
- Carrinho de compras com ajuste de quantidade e total em tempo real
- Formas de pagamento: **Dinheiro** (com cálculo de troco), **PIX**, **Crédito**, **Débito**
- Finalização de venda com dedução de estoque em **transação atômica**
- Número de pedido gerado automaticamente (sequência SERIAL)

### Histórico de Pedidos
- Filtros por período: Hoje, Semana, Mês, Todos
- Estatísticas de quantidade vendida e faturamento
- Busca de pedido por número
- Visualização de itens por pedido

### Gerenciamento de Produtos *(admin)*
- CRUD completo com foto armazenada em `BYTEA` no banco
- Configuração de preço, desconto (0–100%) e estoque
- Código de barras único por produto
- Status Ativo / Inativo
- Proteção contra exclusão de produto com pedidos vinculados

### Gerenciamento de Usuários *(admin)*
- CRUD completo com foto de perfil
- Atribuição de perfil (admin / usuario)
- Alteração de senha independente do cadastro
- Proteção contra auto-exclusão do usuário logado

### Relatórios (FastReport)
- **Cupom de Venda** gerado automaticamente ao finalizar uma venda
- **Relatório de Fechamento de Caixa** gerado ao fechar o caixa
- Exportação automática para **PDF** — abre no visualizador padrão do Windows (Edge, Adobe, etc.)
- Impressão disponível diretamente pelo visualizador de PDF

---

## Arquitetura

O projeto segue arquitetura em **camadas separadas**:

```
PDV_CAIXA/
├── Config/              # Leitura de configurações (appsettings.json)
├── Converters/          # Conversores WPF (byte[] → BitmapImage)
├── Data/                # Fábrica de conexão com o banco
├── Database/
│   ├── Docs/            # Documentação das telas
│   └── Scripts/         # Scripts SQL de criação e migração
├── Models/              # Entidades do domínio
├── Repositories/        # Acesso a dados via Dapper (SQL explícito)
├── Services/            # Regras de negócio e orquestração
├── ViewModels/          # Modelos para binding com a UI
└── Views/               # Janelas XAML (code-behind)
```

| Camada | Responsabilidade |
|---|---|
| `Models/` | Somente propriedades, sem lógica |
| `Repositories/` | Somente SQL — sem regras de negócio |
| `Services/` | Regras de negócio, hash de senha, validações |
| `ViewModels/` | Dados moldados para exibição na tela |
| `Views/` | Interface — chama `Service`, nunca `Repository` diretamente |

---

## Schema do Banco de Dados

```sql
-- Usuários do sistema
usuarios (
    id     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome   VARCHAR(100) UNIQUE NOT NULL,
    senha  VARCHAR(255) NOT NULL,
    perfil VARCHAR(20) DEFAULT 'usuario' CHECK (perfil IN ('admin', 'usuario')),
    foto   BYTEA NULL
)

-- Produtos do catálogo
produtos (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome          VARCHAR(100) NOT NULL,
    descricao     TEXT NULL,
    codigo_barras VARCHAR(50) UNIQUE NULL,
    preco         NUMERIC(10,2) NOT NULL DEFAULT 0,
    desconto      NUMERIC(5,2) NOT NULL DEFAULT 0 CHECK (desconto BETWEEN 0 AND 100),
    estoque       INTEGER NOT NULL DEFAULT 0,
    ativo         BOOLEAN NOT NULL DEFAULT true,
    foto          BYTEA NULL
)

-- Pedidos (cabeçalho)
pedidos (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    numero          SERIAL,
    data            TIMESTAMPTZ NOT NULL DEFAULT now(),
    total           NUMERIC(10,2) NOT NULL DEFAULT 0,
    usuario_id      UUID NOT NULL REFERENCES usuarios(id),
    status          VARCHAR(20) DEFAULT 'finalizado' CHECK (status IN ('finalizado', 'cancelado')),
    forma_pagamento VARCHAR(20) DEFAULT 'dinheiro' CHECK (forma_pagamento IN ('pix', 'dinheiro', 'credito', 'debito'))
)

-- Itens do pedido
pedido_itens (
    id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pedido_id      UUID NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
    produto_id     UUID NOT NULL REFERENCES produtos(id),
    nome_produto   VARCHAR(100) NOT NULL,
    quantidade     INTEGER NOT NULL CHECK (quantidade > 0),
    preco_unitario NUMERIC(10,2) NOT NULL,
    subtotal       NUMERIC(10,2) NOT NULL
)
```

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 13+](https://www.postgresql.org/download/)
- Windows 10 ou superior (WPF)
- Visual Studio 2022 (ou VS Code com extensão C#)

---

## Configuração

### 1. Banco de Dados

Execute os scripts da pasta `PDV_CAIXA/Database/Scripts/` na seguinte ordem:

```bash
# Conecte ao PostgreSQL e execute:
\i criar_banco.sql             # Cria o banco pdv_wpf
\i create_usuarios.sql         # Tabela de usuários + admin padrão
\i create_produtos.sql         # Tabela de produtos + dados de exemplo
\i create_pedidos.sql          # Tabelas de pedidos e itens
\i add_forma_pagamento.sql     # Coluna forma_pagamento em pedidos
\i add_numero_pedidos.sql      # Sequência de número de pedido
\i add_desconto_produto.sql    # Coluna desconto em produtos
```

O script `create_usuarios.sql` cria um administrador padrão:

| Campo | Valor |
|---|---|
| Nome | admin |
| Senha | 1234 |
| Perfil | admin |

### 2. String de Conexão

Edite `PDV_CAIXA/appsettings.json` com suas credenciais:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pdv_wpf;Username=postgres;Password=SUA_SENHA"
  }
}
```

### 3. Executar

```bash
cd PDV_CAIXA
dotnet run --project PDV_CAIXA/PDV_CAIXA.csproj
```

Ou abra a solution no **Visual Studio 2022** e pressione `F5`.

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
        MainWindow (sidebar com foto de perfil)
            │
            ├── Aba PDV
            │       ├── Buscar produto (nome / código de barras)
            │       ├── Adicionar ao carrinho
            │       ├── Ajustar quantidades
            │       └── Finalizar → PagamentoWindow
            │                           └── Confirmar → deduz estoque + salva pedido
            │
            ├── Aba Histórico
            │       ├── Filtros: Hoje / Semana / Mês / Todos
            │       ├── Estatísticas de faturamento
            │       └── Detalhe de itens por pedido
            │
            ├── Aba Pesquisa
            │       └── Busca de pedido por número
            │
            ├── Produtos (admin) → CadastroProdutoWindow
            │
            ├── Usuários (admin) → CadastroUsuarioWindow
            │
            └── Caixa
                    ├── Abrir Caixa → AbrirCaixaWindow
                    ├── Movimentações (Entrada / Saída / Sangria / Suprimento)
                    ├── Histórico de sessões → HistoricoCaixaWindow
                    └── Fechar Caixa → FechamentoCaixaWindow
                                          └── Confirmar → gera Relatório PDF
```

---

## Telas

| Tela | Descrição |
|---|---|
| `LoginWindow` | Autenticação de usuário |
| `MainWindow` | Dashboard com abas PDV, Histórico, Pesquisa, Caixa, Produtos e Usuários |
| `PagamentoWindow` | Seleção de forma de pagamento e cálculo de troco |
| `AbrirCaixaWindow` | Abertura de sessão de caixa com saldo inicial |
| `FechamentoCaixaWindow` | Fechamento de caixa com conferência física e geração de relatório PDF |
| `HistoricoCaixaWindow` | Histórico de sessões de abertura e fechamento |
| `MovimentacaoWindow` | Registro manual de entrada, saída, sangria ou suprimento |
| `CadastroProdutoWindow` | Formulário de criação/edição de produto com foto |
| `CadastroUsuarioWindow` | Formulário de criação/edição de usuário com foto |

---

## Segurança

- Senhas armazenadas com **BCrypt** (custo 11) — nunca em texto puro
- Verificação de senha via `BCrypt.Verify()` — sem comparar strings diretamente
- Queries parametrizadas via Dapper — protegido contra **SQL Injection**
- Connection string fora do código-fonte (`appsettings.json`)
- Controle de acesso por perfil na UI e nas operações
- Fotos armazenadas no banco (sem paths externos expostos)

---

## Pacotes NuGet

```xml
<PackageReference Include="BCrypt.Net-Next"                       Version="4.1.0"    />
<PackageReference Include="Dapper"                                Version="2.1.72"   />
<PackageReference Include="Npgsql"                                Version="10.0.2"   />
<PackageReference Include="FastReport.OpenSource"                 Version="2026.1.8" />
<PackageReference Include="FastReport.OpenSource.Export.PdfSimple" Version="2026.1.8" />
```

> Os pacotes FastReport são instalados automaticamente via NuGet — **não é necessário baixar nada separadamente**.

---

## Relatórios

O sistema utiliza **FastReport.OpenSource 2026.1.8** para geração de relatórios em PDF.

### Cupom de Venda (80 mm — bobina térmica)

Gerado automaticamente ao finalizar uma venda. Contém:

- Número e data do pedido
- Nome do operador
- Lista de itens com quantidade e valor
- Formas de pagamento utilizadas
- Troco (quando aplicável)
- Total da venda

**Como gerar:** Finalize uma venda no PDV → clique **Sim** na pergunta "Deseja imprimir o cupom?"

### Relatório de Fechamento de Caixa (A4)

Gerado ao encerrar uma sessão de caixa. Contém:

- Operador, data/hora de abertura e fechamento, duração da sessão
- Resumo financeiro por forma de pagamento (Dinheiro, Crédito, Débito, PIX)
- Total geral da sessão
- Conferência de caixa: saldo esperado × saldo contado × diferença
- Tabela completa de todas as movimentações da sessão

**Como gerar:** Feche o caixa → clique **Sim** na pergunta "Deseja imprimir o relatório de fechamento?"

### Funcionamento técnico

```
Services/RelatorioService.cs
    ├── ImprimirCupomVenda()        → relatório 80mm
    └── ImprimirFechamentoCaixa()   → relatório A4
            │
            └── ExportarPdf()
                    ├── report.Prepare()              (FastReport processa os dados)
                    ├── report.Export(PDFSimpleExport) (gera o arquivo PDF em /temp)
                    └── Process.Start()               (abre no visualizador padrão do Windows)
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
