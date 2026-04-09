using System.IO;
using System.Text.Json;

namespace PDV_CAIXA.Config {
    public static class AppConfig {
        private static string? _connectionString;

        private static string ArquivoPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public static string ConnectionString {
            get {
                if (_connectionString != null) return _connectionString;
                _connectionString = Ler();
                return _connectionString;
            }
        }

        private static string Ler() {
            var json = File.ReadAllText(ArquivoPath);
            var doc  = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString()
                ?? throw new InvalidOperationException("Connection string não encontrada.");
        }

        public static void Salvar(string host, string port, string database, string username, string password) {
            var conn = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
            var json = JsonSerializer.Serialize(
                new { ConnectionStrings = new { DefaultConnection = conn } },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ArquivoPath, json);
            _connectionString = conn; // atualiza cache
        }
    }
}
