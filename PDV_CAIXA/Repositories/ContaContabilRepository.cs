using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class ContaContabilRepository {
        private readonly Conexao _conexao = new();

        public IEnumerable<ContaContabil> ObterTodos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<ContaContabil>(
                @"SELECT id, codigo_contabil, codigo_reduzido, descricao, grupo, tipo,
                         codigo_historico, historico, grupo_contabil_entrada,
                         grupo_contabil_saida, centro_de_custo, exibir_em_lancamentos_manuais
                  FROM contas_contabeis ORDER BY codigo_contabil");
        }

        public ContaContabil? ObterPorId(int id) {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<ContaContabil>(
                @"SELECT id, codigo_contabil, codigo_reduzido, descricao, grupo, tipo,
                         codigo_historico, historico, grupo_contabil_entrada,
                         grupo_contabil_saida, centro_de_custo, exibir_em_lancamentos_manuais
                  FROM contas_contabeis WHERE id = @Id",
                new { Id = id });
        }

        public void Inserir(ContaContabil conta) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"INSERT INTO contas_contabeis
                    (codigo_contabil, codigo_reduzido, descricao, grupo, tipo,
                     codigo_historico, historico, grupo_contabil_entrada,
                     grupo_contabil_saida, centro_de_custo, exibir_em_lancamentos_manuais)
                  VALUES
                    (@CodigoContabil, @CodigoReduzido, @Descricao, @Grupo, @Tipo,
                     @CodigoHistorico, @Historico, @GrupoContabilEntrada,
                     @GrupoContabilSaida, @CentroDeCusto, @ExibirEmLancamentosManuais)",
                conta);
        }

        public void Atualizar(ContaContabil conta) {
            using var conn = _conexao.CriarConexao();
            conn.Execute(
                @"UPDATE contas_contabeis SET
                    codigo_contabil             = @CodigoContabil,
                    codigo_reduzido             = @CodigoReduzido,
                    descricao                   = @Descricao,
                    grupo                       = @Grupo,
                    tipo                        = @Tipo,
                    codigo_historico            = @CodigoHistorico,
                    historico                   = @Historico,
                    grupo_contabil_entrada      = @GrupoContabilEntrada,
                    grupo_contabil_saida        = @GrupoContabilSaida,
                    centro_de_custo             = @CentroDeCusto,
                    exibir_em_lancamentos_manuais = @ExibirEmLancamentosManuais
                  WHERE id = @Id",
                conta);
        }

        public void Excluir(int id) {
            using var conn = _conexao.CriarConexao();
            conn.Execute("DELETE FROM contas_contabeis WHERE id = @Id", new { Id = id });
        }

        public IEnumerable<string> ObterGrupos() {
            using var conn = _conexao.CriarConexao();
            return conn.Query<string>(
                "SELECT DISTINCT grupo FROM contas_contabeis WHERE grupo IS NOT NULL ORDER BY grupo");
        }
    }
}
