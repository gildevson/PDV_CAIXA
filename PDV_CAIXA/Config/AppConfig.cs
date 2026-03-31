using System.IO;
using System.Text.Json;

namespace PDV_CAIXA.Config {
    public static class AppConfig {
        private static string? _connectionString;

        public static string ConnectionString {
            get {
                if (_connectionString != null) return _connectionString;

                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                var json = File.ReadAllText(path);
                var doc  = JsonDocument.Parse(json);

                _connectionString = doc.RootElement
                    .GetProperty("ConnectionStrings")
                    .GetProperty("DefaultConnection")
                    .GetString()
                    ?? throw new InvalidOperationException("Connection string não encontrada no appsettings.json.");

                return _connectionString;
            }
        }
    }
}
