namespace PDV_CAIXA.ViewModels {
    public class ProdutoViewModel {
        public Guid    Id           { get; set; }
        public string  Nome         { get; set; } = string.Empty;
        public string? Descricao    { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal Preco        { get; set; }
        public decimal Desconto     { get; set; }
        public int     Estoque      { get; set; }
        public bool    Ativo        { get; set; }
        public byte[]? Foto         { get; set; }

        public decimal PrecoComDesconto => Desconto > 0 ? Preco * (1 - Desconto / 100m) : Preco;

        public string PrecoTexto    => Desconto > 0
            ? $"{PrecoComDesconto.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))} (-{Desconto:F0}%)"
            : Preco.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
        public string AtivoTexto    => Ativo ? "Ativo" : "Inativo";
        public string EstoqueTexto  => Estoque.ToString();
        public string DescontoTexto => Desconto > 0 ? $"{Desconto:F0}% OFF" : string.Empty;

        public string Iniciais {
            get {
                if (string.IsNullOrWhiteSpace(Nome)) return "?";
                var partes = Nome.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                return partes.Length >= 2
                    ? $"{partes[0][0]}{partes[^1][0]}".ToUpper()
                    : Nome.Length >= 2 ? Nome[..2].ToUpper() : Nome.ToUpper();
            }
        }
    }
}
