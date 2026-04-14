namespace PDV_CAIXA.Models {
    public class ContaContabil {
        public int     Id                        { get; set; }
        public string  CodigoContabil            { get; set; } = string.Empty;
        public string  CodigoReduzido            { get; set; } = string.Empty;
        public string  Descricao                 { get; set; } = string.Empty;
        public string? Grupo                     { get; set; }
        public string? Tipo                      { get; set; }
        public string? CodigoHistorico           { get; set; }
        public string? Historico                 { get; set; }
        public string? GrupoContabilEntrada      { get; set; }
        public string? GrupoContabilSaida        { get; set; }
        public string? CentroDeCusto             { get; set; }
        public bool    ExibirEmLancamentosManuais { get; set; } = true;
    }
}
