namespace PDV_CAIXA.ViewModels {
    public class ProdutoViewModel {
        public Guid    Id           { get; set; }
        public string  Nome         { get; set; } = string.Empty;
        public string? Descricao    { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal Preco        { get; set; }
        public int     Estoque      { get; set; }
        public bool    Ativo        { get; set; }
        public byte[]? Foto         { get; set; }

        public string PrecoTexto   => Preco.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
        public string AtivoTexto   => Ativo ? "Ativo" : "Inativo";
        public string EstoqueTexto => Estoque.ToString();

        public string Iniciais {
            get {
                var partes = Nome.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                return partes.Length >= 2
                    ? $"{partes[0][0]}{partes[^1][0]}".ToUpper()
                    : Nome.Length >= 2 ? Nome[..2].ToUpper() : Nome.ToUpper();
            }
        }
    }
}
