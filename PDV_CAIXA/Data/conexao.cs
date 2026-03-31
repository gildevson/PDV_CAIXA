using Npgsql;
using PDV_CAIXA.Config;
using System.Data;

namespace PDV_CAIXA.Data {
    public class Conexao {
        public IDbConnection CriarConexao() =>
            new NpgsqlConnection(AppConfig.ConnectionString);
    }
}
