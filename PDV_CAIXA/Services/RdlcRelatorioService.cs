using System.Data;
using System.IO;
using AspNetCore.Reporting;

namespace PDV_CAIXA.Services;

/// <summary>Serviço para renderizar relatórios RDLC em PDF.</summary>
public class RdlcRelatorioService
{
    // Pasta onde ficam os .rdlc (copiados para o output pelo csproj)
    private static string PastaRelatorios =>
        Path.Combine(AppContext.BaseDirectory, "Relatorios");

    /// <summary>
    /// Renderiza um RDLC e retorna os bytes do PDF gerado.
    /// </summary>
    /// <param name="nomeArquivoRdlc">Ex: "RelatorioProdutos.rdlc"</param>
    /// <param name="nomeDataSet">Nome do DataSet declarado no RDLC (null se não houver dados)</param>
    /// <param name="dados">DataTable com os dados</param>
    /// <param name="parametros">Parâmetros do relatório (chave = nome do parâmetro no RDLC)</param>
    public byte[] GerarPdfBytes(
        string nomeArquivoRdlc,
        string? nomeDataSet = null,
        DataTable? dados = null,
        Dictionary<string, string>? parametros = null)
    {
        var caminhoRdlc = Path.Combine(PastaRelatorios, nomeArquivoRdlc);

        if (!File.Exists(caminhoRdlc))
            throw new FileNotFoundException($"Relatório não encontrado: {caminhoRdlc}");

        var report = new LocalReport(caminhoRdlc);

        if (nomeDataSet is not null && dados is not null)
            report.AddDataSource(nomeDataSet, dados);

        var parametrosRdlc = parametros ?? new Dictionary<string, string>();

        var resultado = report.Execute(RenderType.Pdf, 1, parametrosRdlc);
        return resultado.MainStream;
    }
}
