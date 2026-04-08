using System.Data;
using System.IO;
using Microsoft.Reporting.NETCore;

namespace PDV_CAIXA.Services;

/// <summary>Serviço para renderizar relatórios RDLC em PDF.</summary>
public class RdlcRelatorioService
{
    private static string PastaRelatorios =>
        Path.Combine(AppContext.BaseDirectory, "Relatorios");

    /// <summary>
    /// Renderiza um RDLC e retorna os bytes do PDF gerado.
    /// </summary>
    public byte[] GerarPdfBytes(
        string nomeArquivoRdlc,
        string? nomeDataSet = null,
        DataTable? dados = null,
        Dictionary<string, string>? parametros = null)
    {
        var caminhoRdlc = Path.Combine(PastaRelatorios, nomeArquivoRdlc);

        if (!File.Exists(caminhoRdlc))
            throw new FileNotFoundException($"Relatório não encontrado: {caminhoRdlc}");

        var report = new LocalReport();
        report.ReportPath = caminhoRdlc;

        if (nomeDataSet is not null && dados is not null)
            report.DataSources.Add(new ReportDataSource(nomeDataSet, dados));

        if (parametros is { Count: > 0 })
            report.SetParameters(parametros.Select(p => new ReportParameter(p.Key, p.Value)));

        return report.Render("PDF");
    }
}
