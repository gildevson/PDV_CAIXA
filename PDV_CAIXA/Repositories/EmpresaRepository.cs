using Dapper;
using PDV_CAIXA.Data;
using PDV_CAIXA.Models;

namespace PDV_CAIXA.Repositories {
    public class EmpresaRepository {
        private readonly Conexao _conexao = new();

        private const string SelectCols =
            "id, razao_social, nome_fantasia, cnpj, inscricao_est, telefone, email, website, " +
            "cep, logradouro, numero, complemento, bairro, cidade, uf";

        public Empresa Obter() {
            using var conn = _conexao.CriarConexao();
            return conn.QueryFirstOrDefault<Empresa>(
                       $"SELECT {SelectCols} FROM empresa LIMIT 1")
                   ?? new Empresa();
        }

        public void Salvar(Empresa e) {
            using var conn = _conexao.CriarConexao();
            var existe = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM empresa") > 0;
            if (existe) {
                conn.Execute(@"
                    UPDATE empresa SET
                        razao_social  = @RazaoSocial,
                        nome_fantasia = @NomeFantasia,
                        cnpj          = @Cnpj,
                        inscricao_est = @InscricaoEst,
                        telefone      = @Telefone,
                        email         = @Email,
                        website       = @Website,
                        cep           = @Cep,
                        logradouro    = @Logradouro,
                        numero        = @Numero,
                        complemento   = @Complemento,
                        bairro        = @Bairro,
                        cidade        = @Cidade,
                        uf            = @Uf
                    WHERE id = @Id", e);
            } else {
                e.Id = Guid.NewGuid();
                conn.Execute(@"
                    INSERT INTO empresa
                        (id, razao_social, nome_fantasia, cnpj, inscricao_est,
                         telefone, email, website,
                         cep, logradouro, numero, complemento, bairro, cidade, uf)
                    VALUES
                        (@Id, @RazaoSocial, @NomeFantasia, @Cnpj, @InscricaoEst,
                         @Telefone, @Email, @Website,
                         @Cep, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @Uf)", e);
            }
        }
    }
}
