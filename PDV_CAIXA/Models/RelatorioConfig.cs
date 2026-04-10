namespace PDV_CAIXA.Models {
    public class RelatorioConfig {
        public Guid    Id          { get; set; }
        public string  Nome        { get; set; } = string.Empty;
        public string? Descricao   { get; set; }
        public string  Tipo        { get; set; } = string.Empty;
        public string  NomeArquivo { get; set; } = string.Empty;
        public int     Ordem       { get; set; }
        public bool    Ativo       { get; set; } = true;

        public string AtivoTexto => Ativo ? "Sim" : "Não";
        public string TipoLabel  => Tipo switch {
            "Produtos"       => "Produtos Cadastrados",
            "Usuarios"       => "Usuários Cadastrados",
            "ProdutosAtivos" => "Produtos Ativos",
            _                => Tipo
        };
    }
}
