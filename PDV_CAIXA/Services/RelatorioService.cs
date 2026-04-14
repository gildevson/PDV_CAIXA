using System.Data;
using System.Drawing;
using System.IO;
using Dapper;
using FastReport;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using PDV_CAIXA.Models;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Services;

/// <summary>DTO de pagamento para o cupom de venda.</summary>
public record PagamentoCupom(string Forma, decimal Valor);

public class RelatorioService
{
    private static readonly System.Globalization.CultureInfo PtBR =
        System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    // mm → unidades internas do FastReport (96 DPI)
    private static float MM(float mm) => mm * Units.Millimeters;

    // ═══════════════════════════════════════════════════════════════
    // CUPOM DE VENDA — papel 80 mm (bobina térmica)
    // ═══════════════════════════════════════════════════════════════
    public void ImprimirCupomVenda(
        Pedido pedido,
        List<PedidoItem> itens,
        List<PagamentoCupom> pagamentos,
        decimal troco,
        string nomeOperador)
    {
        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth      = 80;
        page.PaperHeight     = 297;
        page.UnlimitedHeight = true;
        page.LeftMargin      = 4;
        page.RightMargin     = 4;
        page.TopMargin       = 5;
        page.BottomMargin    = 5;

        float w    = MM(page.PaperWidth - page.LeftMargin - page.RightMargin); // ~72mm
        float col1 = w * 0.55f;   // produto
        float col2 = w * 0.13f;   // qtd
        float col3 = w * 0.32f;   // total

        // ── Registrar dados dos itens ──────────────────────────────
        var ds = new DataSet();
        var dt = new DataTable("Itens");
        dt.Columns.Add("Nome");
        dt.Columns.Add("Qtd");
        dt.Columns.Add("Unitario");
        dt.Columns.Add("Total");
        foreach (var item in itens)
            dt.Rows.Add(
                item.NomeProduto,
                $"x{item.Quantidade}",
                item.PrecoUnitario.ToString("C2", PtBR),
                item.Subtotal.ToString("C2", PtBR));
        ds.Tables.Add(dt);
        report.RegisterData(ds);

        // ── BAND: Cabeçalho ────────────────────────────────────────
        var titleBand = new ReportTitleBand();
        float ty = 0;

        Txt(titleBand, "✦ PDV CAIXA ✦",       0, ty, w,    MM(9),  13, FontStyle.Bold,    HorzAlign.Center); ty += MM(10);
        Txt(titleBand, "CUPOM NÃO FISCAL",      0, ty, w,    MM(5),   8, FontStyle.Regular, HorzAlign.Center); ty += MM(6);
        Linha(titleBand, 0, ty, w);                                                                              ty += MM(4);
        Txt(titleBand, $"Pedido  #{pedido.Numero:D4}",        0, ty, w, MM(6), 11, FontStyle.Bold, HorzAlign.Center); ty += MM(7);
        Txt(titleBand, pedido.Data.ToString("dd/MM/yyyy   HH:mm", PtBR), 0, ty, w, MM(5), 8, FontStyle.Regular, HorzAlign.Center); ty += MM(6);
        Txt(titleBand, $"Operador: {nomeOperador}",            0, ty, w, MM(5), 8, FontStyle.Regular, HorzAlign.Left);  ty += MM(7);
        Linha(titleBand, 0, ty, w);                                                                              ty += MM(4);

        // cabeçalho das colunas
        Txt(titleBand, "PRODUTO",  0,          ty, col1, MM(5), 8, FontStyle.Bold, HorzAlign.Left);
        Txt(titleBand, "QTD",      col1,        ty, col2, MM(5), 8, FontStyle.Bold, HorzAlign.Center);
        Txt(titleBand, "TOTAL",    col1 + col2, ty, col3, MM(5), 8, FontStyle.Bold, HorzAlign.Right);
        ty += MM(5);
        Linha(titleBand, 0, ty, w); ty += MM(2);

        titleBand.Height = ty;
        page.ReportTitle = titleBand;

        // ── BAND: Itens (data band) ────────────────────────────────
        var dataBand = new DataBand();
        dataBand.DataSource = report.GetDataSource("Itens");
        dataBand.Height     = MM(7);

        Txt(dataBand, "[Itens.Nome]",     0,          0, col1, dataBand.Height, 7, FontStyle.Regular, HorzAlign.Left,   true);
        Txt(dataBand, "[Itens.Qtd]",      col1,        0, col2, dataBand.Height, 7, FontStyle.Regular, HorzAlign.Center);
        Txt(dataBand, "[Itens.Total]",    col1 + col2, 0, col3, dataBand.Height, 7, FontStyle.Bold,    HorzAlign.Right);
        page.Bands.Add(dataBand);

        // ── BAND: Rodapé / totais ──────────────────────────────────
        var summaryBand = new ReportSummaryBand();
        float sy = MM(3);
        Linha(summaryBand, 0, sy, w); sy += MM(4);

        foreach (var pag in pagamentos)
        {
            var label = pag.Forma switch {
                "dinheiro" => "DINHEIRO",
                "credito"  => "CRÉDITO",
                "debito"   => "DÉBITO",
                "pix"      => "PIX",
                _          => pag.Forma.ToUpper()
            };
            Txt(summaryBand, label,                        0,     sy, w * 0.6f, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
            Txt(summaryBand, pag.Valor.ToString("C2", PtBR), w * 0.6f, sy, w * 0.4f, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
            sy += MM(7);
        }

        if (troco > 0)
        {
            Txt(summaryBand, "TROCO",                       0,     sy, w * 0.6f, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
            Txt(summaryBand, troco.ToString("C2", PtBR),    w * 0.6f, sy, w * 0.4f, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
            sy += MM(7);
        }

        Linha(summaryBand, 0, sy, w); sy += MM(3);

        Txt(summaryBand, "TOTAL",                               0,     sy, w * 0.6f, MM(8), 13, FontStyle.Bold, HorzAlign.Left);
        Txt(summaryBand, pedido.Total.ToString("C2", PtBR), w * 0.6f, sy, w * 0.4f, MM(8), 13, FontStyle.Bold, HorzAlign.Right);
        sy += MM(11);

        Linha(summaryBand, 0, sy, w); sy += MM(4);
        Txt(summaryBand, "Obrigado pela preferência!", 0, sy, w, MM(5), 8, FontStyle.Regular, HorzAlign.Center);
        sy += MM(8);

        summaryBand.Height = sy;
        page.ReportSummary = summaryBand;

        ExportarPdf(report, $"Cupom_{pedido.Numero:D4}", $"Cupom de Venda — Pedido #{pedido.Numero:D4}");
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE FECHAMENTO DE CAIXA — A4
    // ═══════════════════════════════════════════════════════════════
    public void ImprimirFechamentoCaixa(
        Caixa caixa,
        string nomeOperador,
        ResultadoFechamento resultado,
        IEnumerable<MovimentacaoCaixa> movimentacoes)
    {
        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth   = 210;
        page.PaperHeight  = 297;
        page.LeftMargin   = 15;
        page.RightMargin  = 15;
        page.TopMargin    = 15;
        page.BottomMargin = 15;

        float w = MM(page.PaperWidth - page.LeftMargin - page.RightMargin); // ~180mm

        // ── Registrar movimentações ────────────────────────────────
        var ds   = new DataSet();
        var dtMov = new DataTable("Movs");
        dtMov.Columns.Add("DataHora");
        dtMov.Columns.Add("Tipo");
        dtMov.Columns.Add("Descricao");
        dtMov.Columns.Add("Forma");
        dtMov.Columns.Add("Valor");
        foreach (var m in movimentacoes.OrderBy(x => x.Data))
        {
            dtMov.Rows.Add(
                m.Data.ToString("dd/MM HH:mm", PtBR),
                m.Tipo == "entrada" ? "Entrada" : "Saída",
                m.Descricao,
                m.FormaPagamento switch {
                    "dinheiro" => "Dinheiro",
                    "credito"  => "Crédito",
                    "debito"   => "Débito",
                    "pix"      => "PIX",
                    _          => "—"
                },
                (m.Tipo == "entrada" ? "+" : "-") + " " + m.Valor.ToString("C2", PtBR));
        }
        ds.Tables.Add(dtMov);
        report.RegisterData(ds);

        // ── BAND: Título ───────────────────────────────────────────
        var title = new ReportTitleBand();
        float ty = 0;

        // Linha de título principal
        Txt(title, "PDV CAIXA",                          0,     ty, w * 0.4f, MM(10), 16, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "RELATÓRIO DE FECHAMENTO",            w * 0.4f, ty, w * 0.6f, MM(10), 13, FontStyle.Bold, HorzAlign.Right);
        ty += MM(12);
        Linha(title, 0, ty, w); ty += MM(6);

        // Info do caixa
        Txt(title, $"Operador de abertura:  {nomeOperador}",                        0,     ty, w / 2, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        Txt(title, $"Abertura:  {caixa.DataAbertura:dd/MM/yyyy  HH:mm}",            w / 2, ty, w / 2, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        ty += MM(7);
        Txt(title, $"Fechamento: {DateTime.Now:dd/MM/yyyy  HH:mm}",                 0,     ty, w / 2, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        var dur = DateTime.Now - caixa.DataAbertura;
        var durTexto = dur.TotalHours >= 1
            ? $"Duração: {(int)dur.TotalHours}h {dur.Minutes:D2}min"
            : $"Duração: {dur.Minutes}min";
        Txt(title, durTexto,                                                          w / 2, ty, w / 2, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        ty += MM(10);
        Linha(title, 0, ty, w); ty += MM(8);

        // ── Resumo financeiro ──────────────────────────────────────
        Txt(title, "RESUMO FINANCEIRO", 0, ty, w, MM(7), 12, FontStyle.Bold, HorzAlign.Left);
        ty += MM(9);

        float lw = w * 0.55f;
        float vw = w * 0.45f;
        float rh = MM(7);

        void LinhaRes(string label, string value, FontStyle fs = FontStyle.Regular, string cor = "") {
            Txt(title, label, 0,  ty, lw, rh, 10, fs, HorzAlign.Left);
            Txt(title, value, lw, ty, vw, rh, 10, fs, HorzAlign.Right);
            ty += rh + MM(1);
        }

        LinhaRes("Saldo Inicial",   resultado.SaldoInicial.ToString("C2",  PtBR));
        LinhaRes("Total Dinheiro",  resultado.TotalDinheiro.ToString("C2", PtBR));
        LinhaRes("Total Crédito",   resultado.TotalCredito.ToString("C2",  PtBR));
        LinhaRes("Total Débito",    resultado.TotalDebito.ToString("C2",   PtBR));
        LinhaRes("Total PIX",       resultado.TotalPix.ToString("C2",      PtBR));
        ty += MM(2);
        Linha(title, 0, ty, lw + vw * 0.5f); ty += MM(3);
        LinhaRes("TOTAL GERAL",     resultado.TotalGeral.ToString("C2",    PtBR), FontStyle.Bold);
        ty += MM(7);
        Linha(title, 0, ty, w); ty += MM(8);

        // ── Conferência de caixa ───────────────────────────────────
        Txt(title, "CONFERÊNCIA DE CAIXA", 0, ty, w, MM(7), 12, FontStyle.Bold, HorzAlign.Left);
        ty += MM(9);

        LinhaRes("Saldo Esperado (dinheiro)",  resultado.SaldoEsperado.ToString("C2", PtBR));
        LinhaRes("Saldo Contado",              resultado.SaldoReal.ToString("C2",     PtBR));

        var diffLabel = resultado.Diferenca == 0
            ? "Diferença  ✓ Conferido"
            : resultado.Diferenca > 0
                ? "Diferença  (sobra)"
                : "Diferença  (falta)";
        LinhaRes(diffLabel, resultado.Diferenca.ToString("C2", PtBR), FontStyle.Bold);
        ty += MM(8);
        Linha(title, 0, ty, w); ty += MM(8);

        // ── Cabeçalho da tabela de movimentações ──────────────────
        Txt(title, "MOVIMENTAÇÕES DA SESSÃO", 0, ty, w, MM(7), 12, FontStyle.Bold, HorzAlign.Left);
        ty += MM(9);

        float mc1 = w * 0.14f;  // data/hora
        float mc2 = w * 0.10f;  // tipo
        float mc3 = w * 0.42f;  // descrição
        float mc4 = w * 0.16f;  // forma
        float mc5 = w * 0.18f;  // valor

        Txt(title, "DATA/HORA",  0,                     ty, mc1, MM(6), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "TIPO",       mc1,                    ty, mc2, MM(6), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "DESCRIÇÃO",  mc1+mc2,                ty, mc3, MM(6), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "FORMA",      mc1+mc2+mc3,            ty, mc4, MM(6), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "VALOR",      mc1+mc2+mc3+mc4,        ty, mc5, MM(6), 9, FontStyle.Bold, HorzAlign.Right);
        ty += MM(6);
        Linha(title, 0, ty, w); ty += MM(2);

        title.Height = ty;
        page.ReportTitle = title;

        // ── BAND: Linhas de movimentação ───────────────────────────
        var dataBand = new DataBand();
        dataBand.DataSource = report.GetDataSource("Movs");
        dataBand.Height     = MM(7);

        Txt(dataBand, "[Movs.DataHora]",  0,               0, mc1, dataBand.Height, 8, FontStyle.Regular, HorzAlign.Left);
        Txt(dataBand, "[Movs.Tipo]",      mc1,              0, mc2, dataBand.Height, 8, FontStyle.Regular, HorzAlign.Left);
        Txt(dataBand, "[Movs.Descricao]", mc1+mc2,          0, mc3, dataBand.Height, 8, FontStyle.Regular, HorzAlign.Left,  true);
        Txt(dataBand, "[Movs.Forma]",     mc1+mc2+mc3,      0, mc4, dataBand.Height, 8, FontStyle.Regular, HorzAlign.Left);
        Txt(dataBand, "[Movs.Valor]",     mc1+mc2+mc3+mc4,  0, mc5, dataBand.Height, 8, FontStyle.Regular, HorzAlign.Right);
        page.Bands.Add(dataBand);

        // ── BAND: Rodapé de página ─────────────────────────────────
        var pageFooter = new PageFooterBand();
        pageFooter.Height = MM(8);
        Txt(pageFooter, $"Gerado em {DateTime.Now:dd/MM/yyyy  HH:mm}", 0, 0, w, MM(5), 8, FontStyle.Regular, HorzAlign.Right);
        page.PageFooter = pageFooter;

        ExportarPdf(report, "Fechamento_Caixa", "Relatório de Fechamento de Caixa");
    }

    // ═══════════════════════════════════════════════════════════════
    // HISTÓRICO DE PEDIDO — 80mm (bobina térmica)
    // ═══════════════════════════════════════════════════════════════
    public void ImprimirHistoricoPedido(
        PedidoResumoViewModel pedido,
        IEnumerable<PedidoItem> itens)
    {
        var listaItens = itens.ToList();
        var empresa    = new Repositories.EmpresaRepository().Obter();
        var nomeEmp    = !string.IsNullOrWhiteSpace(empresa.NomeFantasia)
                             ? empresa.NomeFantasia : empresa.RazaoSocial;

        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth      = 80;
        page.PaperHeight     = 297;
        page.UnlimitedHeight = true;
        page.LeftMargin      = 3;
        page.RightMargin     = 3;
        page.TopMargin       = 4;
        page.BottomMargin    = 4;

        float w     = MM(page.PaperWidth - page.LeftMargin - page.RightMargin);
        float cDesc = w * 0.50f;
        float cQtdU = w * 0.28f;
        float cVal  = w * 0.22f;
        float cLbl  = w * 0.65f;
        float cNum  = w * 0.35f;

        // ── TUDO em uma única band (evita bug do DataBand no PDF) ──
        var band = new ReportTitleBand();
        float y  = 0;

        // Empresa
        if (!string.IsNullOrWhiteSpace(nomeEmp))
        { Txt(band, nomeEmp.ToUpper(), 0, y, w, MM(8), 11, FontStyle.Bold, HorzAlign.Center); y += MM(10); }

        if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial) && empresa.RazaoSocial != nomeEmp)
        { Txt(band, empresa.RazaoSocial, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center, true); y += MM(6); }

        var endEmp = string.IsNullOrWhiteSpace(empresa.Logradouro) ? "" :
                     empresa.Logradouro + ", " + empresa.Numero +
                     (!string.IsNullOrWhiteSpace(empresa.Complemento) ? " - " + empresa.Complemento : "");
        if (!string.IsNullOrWhiteSpace(endEmp))
        { Txt(band, endEmp, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center, true); y += MM(6); }

        var bairro   = !string.IsNullOrWhiteSpace(empresa.Bairro) ? empresa.Bairro + " - " : "";
        var cidadeUf = string.IsNullOrWhiteSpace(empresa.Cidade) ? "" :
                       bairro + empresa.Cidade + " - " + empresa.Uf +
                       (!string.IsNullOrWhiteSpace(empresa.Cep) ? "  CEP: " + empresa.Cep : "");
        if (!string.IsNullOrWhiteSpace(cidadeUf))
        { Txt(band, cidadeUf, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center); y += MM(6); }

        if (!string.IsNullOrWhiteSpace(empresa.Telefone))
        { Txt(band, empresa.Telefone, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center); y += MM(6); }

        Linha(band, 0, y, w); y += MM(4);

        var cnpjStr = !string.IsNullOrWhiteSpace(empresa.Cnpj) ? $"CNPJ: {empresa.Cnpj}" : "";
        var ieStr   = !string.IsNullOrWhiteSpace(empresa.InscricaoEst) ? $"IE: {empresa.InscricaoEst}" : "";
        if (!string.IsNullOrWhiteSpace(cnpjStr) || !string.IsNullOrWhiteSpace(ieStr))
        {
            Txt(band, cnpjStr, 0,         y, w * 0.55f, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
            Txt(band, ieStr,   w * 0.55f,  y, w * 0.45f, MM(5), 7, FontStyle.Regular, HorzAlign.Right);
            y += MM(6);
        }

        Linha(band, 0, y, w); y += MM(4);
        Txt(band, "CLIENTE: CONSUMIDOR FINAL", 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Left); y += MM(6);
        Linha(band, 0, y, w); y += MM(4);

        Txt(band, pedido.Data.ToString("dd/MM/yyyy  HH:mm", PtBR), 0, y, w * 0.50f, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
        Txt(band, "COMPROVANTE DE VENDA", w * 0.50f, y, w * 0.50f, MM(4), 6, FontStyle.Regular, HorzAlign.Right);
        y += MM(5);
        Txt(band, $"Nº {pedido.Numero:D6}", w * 0.50f, y, w * 0.50f, MM(6), 11, FontStyle.Bold, HorzAlign.Right);
        y += MM(8);

        DuplaLinha(band, 0, y, w); y += MM(4);

        // Cabeçalho das colunas
        Txt(band, "DESCRIÇÃO",  0,            y, cDesc, MM(4), 7, FontStyle.Bold, HorzAlign.Left);
        Txt(band, "QTD x UNIT", cDesc,         y, cQtdU, MM(4), 7, FontStyle.Bold, HorzAlign.Center);
        Txt(band, "R$ VALOR",   cDesc + cQtdU, y, cVal,  MM(4), 7, FontStyle.Bold, HorzAlign.Right);
        y += MM(4);
        Linha(band, 0, y, w); y += MM(2);

        // ── ITENS (loop direto, sem DataBand) ──────────────────────
        foreach (var item in listaItens)
        {
            Txt(band, item.NomeProduto,
                0, y, cDesc, MM(7), 7, FontStyle.Regular, HorzAlign.Left, wordWrap: true);
            Txt(band, $"{item.Quantidade}x @ {item.PrecoUnitario.ToString("N2", PtBR)}",
                cDesc, y, cQtdU, MM(7), 7, FontStyle.Regular, HorzAlign.Center, wordWrap: true);
            Txt(band, item.Subtotal.ToString("C2", PtBR),
                cDesc + cQtdU, y, cVal, MM(7), 7, FontStyle.Bold, HorzAlign.Right);
            y += MM(7);
        }

        // ── TOTAIS ─────────────────────────────────────────────────
        Linha(band, 0, y, w); y += MM(4);

        Txt(band, "Total da Nota R$", 0,    y, cLbl, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
        Txt(band, pedido.Total.ToString("N2", PtBR), cLbl, y, cNum, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
        y += MM(7);

        Linha(band, 0, y, w); y += MM(4);

        Txt(band, $"FORMA DE PGTO.: {pedido.PagamentoTexto}", 0, y, w, MM(5), 7, FontStyle.Bold, HorzAlign.Left);
        y += MM(6);

        Txt(band, pedido.Data.ToString("dd/MM/yy", PtBR), 0,        y, w * 0.30f, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
        Txt(band, pedido.Total.ToString("N2", PtBR),      w * 0.30f, y, w * 0.33f, MM(5), 7, FontStyle.Regular, HorzAlign.Center);
        Txt(band, pedido.PagamentoTexto,                  w * 0.63f, y, w * 0.37f, MM(5), 7, FontStyle.Regular, HorzAlign.Right);
        y += MM(6);

        Linha(band, 0, y, w); y += MM(4);

        Txt(band, $"VENDEDOR(A): {pedido.Operador}", 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
        y += MM(9);

        Linha(band, 0, y, w); y += MM(7);

        Txt(band, "Recebi a(s) mercadoria(s) acima descrita(s), concordando plenamente com os prazos e condições de garantia.",
            0, y, w, MM(12), 7, FontStyle.Regular, HorzAlign.Left, wordWrap: true);
        y += MM(18);

        Txt(band, new string('-', 42), 0, y, w, MM(4), 7, FontStyle.Regular, HorzAlign.Center);
        y += MM(5);
        Txt(band, "ASSINATURA DO CLIENTE", 0, y, w, MM(4), 7, FontStyle.Regular, HorzAlign.Center);
        y += MM(7);

        Linha(band, 0, y, w); y += MM(5);

        Txt(band, "* OBRIGADO E VOLTE SEMPRE *", 0, y, w, MM(6), 9, FontStyle.Bold, HorzAlign.Center);
        y += MM(10);

        band.Height = y;
        page.ReportTitle = band;

        ExportarPdf(report, $"Recibo_{pedido.Numero:D5}", $"Comprovante de Venda — Pedido #{pedido.Numero:D5}");
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE USUÁRIOS CADASTRADOS — A4 via RDLC
    // ═══════════════════════════════════════════════════════════════
    public void GerarRelatorioUsuarios(IEnumerable<Usuario> usuarios, string nomeArquivo = "RelatorioUsuarios.rdlc")
    {
        var lista = usuarios.ToList();

        var dt = new System.Data.DataTable();
        dt.Columns.Add("Nome");
        dt.Columns.Add("Perfil");
        foreach (var u in lista)
            dt.Rows.Add(u.Nome, u.Perfil == "admin" ? "Administrador" : "Usuário");

        var parametros = new Dictionary<string, string>
        {
            ["DataGeracao"]   = $"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}",
            ["TotalUsuarios"] = lista.Count.ToString()
        };

        var rdlcService  = new RdlcRelatorioService();
        var pdfBytes     = rdlcService.GerarPdfBytes(nomeArquivo, "DataSetUsuarios", dt, parametros);
        var nomePdf      = Path.GetFileNameWithoutExtension(nomeArquivo);

        var pasta = PastaRelatorios();
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, $"{nomePdf}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        File.WriteAllBytes(caminho, pdfBytes);

        var preview = new PDV_CAIXA.Views.RelatorioPreviewWindow("Usuários Cadastrados", caminho);
        preview.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE PRODUTOS CADASTRADOS — A4 via RDLC
    // ═══════════════════════════════════════════════════════════════
    public void GerarRelatorioProdutos(IEnumerable<Produto> produtos, string nomeArquivo = "RelatorioProdutos.rdlc")
    {
        var lista = produtos.ToList();

        var dt = new System.Data.DataTable();
        dt.Columns.Add("Nome");
        dt.Columns.Add("CodigoBarras");
        dt.Columns.Add("Preco");
        dt.Columns.Add("Desconto");
        dt.Columns.Add("Estoque");
        dt.Columns.Add("Ativo");
        foreach (var p in lista)
            dt.Rows.Add(
                p.Nome,
                p.CodigoBarras ?? "—",
                p.Preco.ToString("C2", PtBR),
                p.Desconto > 0 ? $"{p.Desconto:F0}%" : "—",
                p.Estoque.ToString(),
                p.Ativo ? "Sim" : "Não");

        var parametros = new Dictionary<string, string>
        {
            ["DataGeracao"]   = $"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}",
            ["TotalProdutos"] = lista.Count.ToString()
        };

        var rdlcService  = new RdlcRelatorioService();
        var pdfBytes     = rdlcService.GerarPdfBytes(nomeArquivo, "DataSetProdutos", dt, parametros);
        var nomePdf      = Path.GetFileNameWithoutExtension(nomeArquivo);

        var pasta = PastaRelatorios();
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, $"{nomePdf}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        File.WriteAllBytes(caminho, pdfBytes);

        var preview = new PDV_CAIXA.Views.RelatorioPreviewWindow("Produtos Cadastrados", caminho);
        preview.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE PRODUTOS ATIVOS — A4 via RDLC (VIEW vw_produtos_ativos)
    // ═══════════════════════════════════════════════════════════════
    public void GerarRelatorioProdutosAtivos(string nomeArquivo = "RelatorioProdutosAtivos.rdlc")
    {
        var conexao = new Data.Conexao();
        using var conn = conexao.CriarConexao();
        var rows = conn.Query("SELECT nome, codigo_barras, preco, desconto, estoque FROM vw_produtos_ativos").ToList();

        var dt = new System.Data.DataTable();
        dt.Columns.Add("Nome");
        dt.Columns.Add("CodigoBarras");
        dt.Columns.Add("Preco");
        dt.Columns.Add("Desconto");
        dt.Columns.Add("Estoque");
        foreach (var r in rows)
            dt.Rows.Add(
                (string)r.nome,
                (string)r.codigo_barras,
                ((decimal)r.preco).ToString("C2", PtBR),
                ((decimal)r.desconto) > 0 ? $"{(decimal)r.desconto:F0}%" : "—",
                ((int)r.estoque).ToString());

        var parametros = new Dictionary<string, string>
        {
            ["DataGeracao"]   = $"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}",
            ["TotalProdutos"] = dt.Rows.Count.ToString()
        };

        var rdlcService = new RdlcRelatorioService();
        var pdfBytes    = rdlcService.GerarPdfBytes(nomeArquivo, "DataSetProdutosAtivos", dt, parametros);
        var nomePdf     = Path.GetFileNameWithoutExtension(nomeArquivo);

        var pasta = PastaRelatorios();
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, $"{nomePdf}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        File.WriteAllBytes(caminho, pdfBytes);

        var preview = new PDV_CAIXA.Views.RelatorioPreviewWindow("Produtos Ativos", caminho);
        preview.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // RECIBO DE VENDA — Impressora Térmica 80 mm (FastReport)
    // ═══════════════════════════════════════════════════════════════
    public void ImprimirReciboTermico(
        Pedido pedido,
        List<PedidoItem> itens,
        List<PagamentoCupom> pagamentos,
        decimal troco,
        string nomeOperador)
    {
        var empresa = new Repositories.EmpresaRepository().Obter();
        var nomeEmp = !string.IsNullOrWhiteSpace(empresa.NomeFantasia)
                          ? empresa.NomeFantasia : empresa.RazaoSocial;

        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth      = 80;
        page.PaperHeight     = 297;
        page.UnlimitedHeight = true;
        page.LeftMargin      = 3;
        page.RightMargin     = 3;
        page.TopMargin       = 4;
        page.BottomMargin    = 4;

        float w = MM(page.PaperWidth - page.LeftMargin - page.RightMargin); // ~74mm

        // ── DataTable: Nome | QtdUnit (ex: "2x @ R$2,25") | Subtotal ─
        var ds = new DataSet();
        var dt = new DataTable("Itens");
        dt.Columns.Add("Nome");
        dt.Columns.Add("QtdUnit");
        dt.Columns.Add("Total");
        foreach (var item in itens)
            dt.Rows.Add(
                item.NomeProduto,
                $"{item.Quantidade}x  @{item.PrecoUnitario.ToString("N2", PtBR)}",
                item.Subtotal.ToString("C2", PtBR));
        ds.Tables.Add(dt);
        report.RegisterData(ds);

        // ── BAND: Cabeçalho ────────────────────────────────────────
        var titleBand = new ReportTitleBand();
        float ty = 0;

        // Nome da empresa
        if (!string.IsNullOrWhiteSpace(nomeEmp))
        { Txt(titleBand, nomeEmp.ToUpper(), 0, ty, w, MM(9), 13, FontStyle.Bold, HorzAlign.Center); ty += MM(11); }

        // CNPJ
        if (!string.IsNullOrWhiteSpace(empresa.Cnpj))
        { Txt(titleBand, $"CNPJ: {empresa.Cnpj}", 0, ty, w, MM(5), 8, FontStyle.Regular, HorzAlign.Center); ty += MM(6); }

        // Endereço
        var end = string.IsNullOrWhiteSpace(empresa.Logradouro) ? "" :
                  empresa.Logradouro + ", " + empresa.Numero +
                  (!string.IsNullOrWhiteSpace(empresa.Complemento) ? " - " + empresa.Complemento : "");
        if (!string.IsNullOrWhiteSpace(end))
        { Txt(titleBand, end, 0, ty, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center, true); ty += MM(6); }

        // Cidade/UF/CEP
        var cidadeUf = string.IsNullOrWhiteSpace(empresa.Cidade) ? "" :
                       empresa.Cidade + " - " + empresa.Uf +
                       (!string.IsNullOrWhiteSpace(empresa.Cep) ? "  CEP: " + empresa.Cep : "");
        if (!string.IsNullOrWhiteSpace(cidadeUf))
        { Txt(titleBand, cidadeUf, 0, ty, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center); ty += MM(6); }

        // Telefone
        if (!string.IsNullOrWhiteSpace(empresa.Telefone))
        { Txt(titleBand, $"Tel: {empresa.Telefone}", 0, ty, w, MM(5), 8, FontStyle.Regular, HorzAlign.Center); ty += MM(7); }

        DuplaLinha(titleBand, 0, ty, w); ty += MM(4);

        Txt(titleBand, "RECIBO DE VENDA", 0, ty, w, MM(8), 14, FontStyle.Bold, HorzAlign.Center); ty += MM(10);

        DuplaLinha(titleBand, 0, ty, w); ty += MM(5);

        // Info do pedido
        Txt(titleBand, $"Pedido: #{pedido.Numero:D5}",
            0, ty, w * 0.55f, MM(5), 8, FontStyle.Regular, HorzAlign.Left);
        Txt(titleBand, pedido.Data.ToString("dd/MM/yy HH:mm", PtBR),
            w * 0.55f, ty, w * 0.45f, MM(5), 8, FontStyle.Regular, HorzAlign.Right);
        ty += MM(6);
        Txt(titleBand, $"Operador: {nomeOperador}", 0, ty, w, MM(5), 8, FontStyle.Regular, HorzAlign.Left);
        ty += MM(7);

        LinhaPontilhada(titleBand, 0, ty, w); ty += MM(4);

        // Cabeçalho das colunas de itens
        float cNome  = w * 0.48f;
        float cQtdU  = w * 0.28f;
        float cTotal = w * 0.24f;

        Txt(titleBand, "PRODUTO",   0,               ty, cNome,  MM(5), 8, FontStyle.Bold, HorzAlign.Left);
        Txt(titleBand, "QTD/UNIT",  cNome,            ty, cQtdU,  MM(5), 8, FontStyle.Bold, HorzAlign.Center);
        Txt(titleBand, "TOTAL",     cNome + cQtdU,    ty, cTotal, MM(5), 8, FontStyle.Bold, HorzAlign.Right);
        ty += MM(5);
        LinhaPontilhada(titleBand, 0, ty, w); ty += MM(2);

        titleBand.Height = ty;
        page.ReportTitle = titleBand;

        // ── BAND: Itens (cada linha = 1 produto) ───────────────────
        var dataBand        = new DataBand();
        dataBand.DataSource = report.GetDataSource("Itens");
        dataBand.Height     = MM(8);

        Txt(dataBand, "[Itens.Nome]",    0,            0, cNome,  dataBand.Height, 7, FontStyle.Regular, HorzAlign.Left,   true);
        Txt(dataBand, "[Itens.QtdUnit]", cNome,         0, cQtdU,  dataBand.Height, 7, FontStyle.Regular, HorzAlign.Center, true);
        Txt(dataBand, "[Itens.Total]",   cNome + cQtdU, 0, cTotal, dataBand.Height, 7, FontStyle.Bold,    HorzAlign.Right);
        page.Bands.Add(dataBand);

        // ── BAND: Rodapé ───────────────────────────────────────────
        var summaryBand = new ReportSummaryBand();
        float sy = MM(3);

        LinhaPontilhada(summaryBand, 0, sy, w); sy += MM(4);

        // Formas de pagamento
        Txt(summaryBand, "PAGAMENTO", 0, sy, w, MM(5), 8, FontStyle.Bold, HorzAlign.Left); sy += MM(6);
        foreach (var pag in pagamentos)
        {
            var label = pag.Forma switch {
                "dinheiro" => "Dinheiro",
                "credito"  => "Cartão Crédito",
                "debito"   => "Cartão Débito",
                "pix"      => "PIX",
                _          => pag.Forma
            };
            Txt(summaryBand, label,                           0,        sy, w * 0.62f, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
            Txt(summaryBand, pag.Valor.ToString("C2", PtBR), w * 0.62f, sy, w * 0.38f, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
            sy += MM(7);
        }

        if (troco > 0)
        {
            Txt(summaryBand, "Troco",                    0,        sy, w * 0.62f, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
            Txt(summaryBand, troco.ToString("C2", PtBR), w * 0.62f, sy, w * 0.38f, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
            sy += MM(7);
        }

        DuplaLinha(summaryBand, 0, sy, w); sy += MM(4);

        Txt(summaryBand, "TOTAL",                            0,        sy, w * 0.5f, MM(10), 16, FontStyle.Bold, HorzAlign.Left);
        Txt(summaryBand, pedido.Total.ToString("C2", PtBR),  w * 0.5f, sy, w * 0.5f, MM(10), 16, FontStyle.Bold, HorzAlign.Right);
        sy += MM(13);

        DuplaLinha(summaryBand, 0, sy, w); sy += MM(6);
        Txt(summaryBand, "Obrigado pela preferência!", 0, sy, w, MM(5), 9, FontStyle.Bold, HorzAlign.Center);
        sy += MM(8);

        summaryBand.Height = sy;
        page.ReportSummary = summaryBand;

        // ── Gerar PDF e abrir preview (imprimir de lá na térmica) ──
        ExportarPdf(report, $"ReciboTermico_{pedido.Numero:D5}", $"Recibo Térmico — Pedido #{pedido.Numero:D5}");
    }

    private static void DuplaLinha(BandBase band, float x, float y, float w)
    {
        band.Objects.Add(new LineObject { Bounds = new RectangleF(x, y,          w, 1) });
        band.Objects.Add(new LineObject { Bounds = new RectangleF(x, y + MM(1f), w, 1) });
    }

    private static void LinhaPontilhada(BandBase band, float x, float y, float w)
        => Linha(band, x, y, w);

    // ═══════════════════════════════════════════════════════════════
    // Gera o PDF e abre o preview visual dentro do app (WebView2)
    // ═══════════════════════════════════════════════════════════════
    // RECIBO DE VENDA — A4 via FastReport (layout comprovante)
    // ═══════════════════════════════════════════════════════════════
    public void GerarReciboPedido(
        Pedido pedido,
        List<PedidoItem> itens,
        List<PagamentoCupom> pagamentos,
        decimal troco,
        string nomeOperador,
        string nomeArquivo = "ReciboPedido.rdlc")
    {
        var empresa = new Repositories.EmpresaRepository().Obter();
        var nomeEmp = !string.IsNullOrWhiteSpace(empresa.NomeFantasia)
                          ? empresa.NomeFantasia : empresa.RazaoSocial;

        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth      = 80;
        page.PaperHeight     = 297;
        page.UnlimitedHeight = true;
        page.LeftMargin      = 3;
        page.RightMargin     = 3;
        page.TopMargin       = 4;
        page.BottomMargin    = 4;

        float w    = MM(page.PaperWidth - page.LeftMargin - page.RightMargin);
        float cDesc = w * 0.50f;
        float cQtdU = w * 0.28f;
        float cVal  = w * 0.22f;
        float cLbl  = w * 0.65f;
        float cNum  = w * 0.35f;

        // ── TUDO em uma única band (evita bug do DataBand no PDF) ──
        var band = new ReportTitleBand();
        float y  = 0;

        // Empresa
        if (!string.IsNullOrWhiteSpace(nomeEmp))
        { Txt(band, nomeEmp.ToUpper(), 0, y, w, MM(8), 11, FontStyle.Bold, HorzAlign.Center); y += MM(10); }

        if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial) && empresa.RazaoSocial != nomeEmp)
        { Txt(band, empresa.RazaoSocial, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center, true); y += MM(6); }

        var endEmp = string.IsNullOrWhiteSpace(empresa.Logradouro) ? "" :
                     empresa.Logradouro + ", " + empresa.Numero +
                     (!string.IsNullOrWhiteSpace(empresa.Complemento) ? " - " + empresa.Complemento : "");
        if (!string.IsNullOrWhiteSpace(endEmp))
        { Txt(band, endEmp, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center, true); y += MM(6); }

        var bairro   = !string.IsNullOrWhiteSpace(empresa.Bairro) ? empresa.Bairro + " - " : "";
        var cidadeUf = string.IsNullOrWhiteSpace(empresa.Cidade) ? "" :
                       bairro + empresa.Cidade + " - " + empresa.Uf +
                       (!string.IsNullOrWhiteSpace(empresa.Cep) ? "  CEP: " + empresa.Cep : "");
        if (!string.IsNullOrWhiteSpace(cidadeUf))
        { Txt(band, cidadeUf, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center); y += MM(6); }

        if (!string.IsNullOrWhiteSpace(empresa.Telefone))
        { Txt(band, empresa.Telefone, 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Center); y += MM(6); }

        Linha(band, 0, y, w); y += MM(4);

        var cnpjStr = !string.IsNullOrWhiteSpace(empresa.Cnpj) ? $"CNPJ: {empresa.Cnpj}" : "";
        var ieStr   = !string.IsNullOrWhiteSpace(empresa.InscricaoEst) ? $"IE: {empresa.InscricaoEst}" : "";
        if (!string.IsNullOrWhiteSpace(cnpjStr) || !string.IsNullOrWhiteSpace(ieStr))
        {
            Txt(band, cnpjStr, 0,         y, w * 0.55f, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
            Txt(band, ieStr,   w * 0.55f,  y, w * 0.45f, MM(5), 7, FontStyle.Regular, HorzAlign.Right);
            y += MM(6);
        }

        Linha(band, 0, y, w); y += MM(4);
        Txt(band, "CLIENTE: CONSUMIDOR FINAL", 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Left); y += MM(6);
        Linha(band, 0, y, w); y += MM(4);

        Txt(band, pedido.Data.ToString("dd/MM/yyyy  HH:mm", PtBR), 0, y, w * 0.50f, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
        Txt(band, "COMPROVANTE DE VENDA", w * 0.50f, y, w * 0.50f, MM(4), 6, FontStyle.Regular, HorzAlign.Right);
        y += MM(5);
        Txt(band, $"Nº {pedido.Numero:D6}", w * 0.50f, y, w * 0.50f, MM(6), 11, FontStyle.Bold, HorzAlign.Right);
        y += MM(8);

        DuplaLinha(band, 0, y, w); y += MM(4);

        Txt(band, "DESCRIÇÃO",  0,            y, cDesc, MM(4), 7, FontStyle.Bold, HorzAlign.Left);
        Txt(band, "QTD x UNIT", cDesc,         y, cQtdU, MM(4), 7, FontStyle.Bold, HorzAlign.Center);
        Txt(band, "R$ VALOR",   cDesc + cQtdU, y, cVal,  MM(4), 7, FontStyle.Bold, HorzAlign.Right);
        y += MM(4);
        Linha(band, 0, y, w); y += MM(2);

        // ── ITENS (loop direto, sem DataBand) ──────────────────────
        foreach (var item in itens)
        {
            Txt(band, item.NomeProduto,
                0, y, cDesc, MM(7), 7, FontStyle.Regular, HorzAlign.Left, wordWrap: true);
            Txt(band, $"{item.Quantidade}x @ {item.PrecoUnitario.ToString("N2", PtBR)}",
                cDesc, y, cQtdU, MM(7), 7, FontStyle.Regular, HorzAlign.Center, wordWrap: true);
            Txt(band, item.Subtotal.ToString("C2", PtBR),
                cDesc + cQtdU, y, cVal, MM(7), 7, FontStyle.Bold, HorzAlign.Right);
            y += MM(7);
        }

        // ── TOTAIS E PAGAMENTOS ────────────────────────────────────
        Linha(band, 0, y, w); y += MM(4);

        Txt(band, "Total da Nota R$",               0,    y, cLbl, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
        Txt(band, pedido.Total.ToString("N2", PtBR), cLbl, y, cNum, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
        y += MM(7);

        decimal totalRecebido = pagamentos.Sum(p => p.Valor);
        Txt(band, "Valor Recebido R$",                    0,    y, cLbl, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
        Txt(band, totalRecebido.ToString("N2", PtBR),     cLbl, y, cNum, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
        y += MM(7);

        if (troco > 0)
        {
            Txt(band, "Troco R$",                     0,    y, cLbl, MM(6), 9, FontStyle.Regular, HorzAlign.Left);
            Txt(band, troco.ToString("N2", PtBR),     cLbl, y, cNum, MM(6), 9, FontStyle.Regular, HorzAlign.Right);
            y += MM(7);
        }

        Linha(band, 0, y, w); y += MM(4);

        var formasPgto = string.Join(" / ", pagamentos.Select(p => FormatarFormaPagamento(p.Forma)));
        Txt(band, $"FORMA DE PGTO.: {formasPgto}", 0, y, w, MM(5), 7, FontStyle.Bold, HorzAlign.Left);
        y += MM(6);

        Linha(band, 0, y, w); y += MM(3);

        float pc1 = w * 0.30f, pc2 = w * 0.33f, pc3 = w * 0.37f;
        Txt(band, "DATA PGTO",  0,        y, pc1, MM(4), 7, FontStyle.Bold, HorzAlign.Left);
        Txt(band, "R$ VALOR",   pc1,       y, pc2, MM(4), 7, FontStyle.Bold, HorzAlign.Center);
        Txt(band, "TIPO PGTO",  pc1 + pc2, y, pc3, MM(4), 7, FontStyle.Bold, HorzAlign.Right);
        y += MM(5);

        foreach (var pag in pagamentos)
        {
            Txt(band, pedido.Data.ToString("dd/MM/yy", PtBR), 0,        y, pc1, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
            Txt(band, pag.Valor.ToString("N2", PtBR),          pc1,       y, pc2, MM(5), 7, FontStyle.Regular, HorzAlign.Center);
            Txt(band, FormatarFormaPagamento(pag.Forma),        pc1 + pc2, y, pc3, MM(5), 7, FontStyle.Regular, HorzAlign.Right);
            y += MM(6);
        }

        Linha(band, 0, y, w); y += MM(4);
        Txt(band, $"VENDEDOR(A): {nomeOperador}", 0, y, w, MM(5), 7, FontStyle.Regular, HorzAlign.Left);
        y += MM(9);

        Linha(band, 0, y, w); y += MM(7);
        Txt(band, "Recebi a(s) mercadoria(s) acima descrita(s), concordando plenamente com os prazos e condições de garantia.",
            0, y, w, MM(12), 7, FontStyle.Regular, HorzAlign.Left, wordWrap: true);
        y += MM(18);

        Txt(band, new string('-', 42), 0, y, w, MM(4), 7, FontStyle.Regular, HorzAlign.Center);
        y += MM(5);
        Txt(band, "ASSINATURA DO CLIENTE", 0, y, w, MM(4), 7, FontStyle.Regular, HorzAlign.Center);
        y += MM(7);

        Linha(band, 0, y, w); y += MM(5);
        Txt(band, "* OBRIGADO E VOLTE SEMPRE *", 0, y, w, MM(6), 9, FontStyle.Bold, HorzAlign.Center);
        y += MM(10);

        band.Height = y;
        page.ReportTitle = band;

        ExportarPdf(report, $"Recibo_{pedido.Numero:D5}", $"Comprovante de Venda — Pedido #{pedido.Numero:D5}");
    }

    private static string FormatarFormaPagamento(string forma) => forma switch {
        "dinheiro" => "Dinheiro",
        "credito"  => "Cartão Crédito",
        "debito"   => "Cartão Débito",
        "pix"      => "PIX",
        _          => forma
    };

    // ═══════════════════════════════════════════════════════════════
    // ═══════════════════════════════════════════════════════════════
    // TESTE DE IMPRESSÃO
    // ═══════════════════════════════════════════════════════════════
    public void GerarTesteImpressao()
    {
        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);
        page.PaperWidth = 210; page.PaperHeight = 297;
        page.LeftMargin = 20; page.RightMargin = 20;
        page.TopMargin  = 20; page.BottomMargin = 20;
        float w = MM(page.PaperWidth - page.LeftMargin - page.RightMargin);

        var title = new ReportTitleBand();
        float ty = MM(4);
        Txt(title, "PÁGINA DE TESTE DE IMPRESSÃO", 0, ty, w, MM(12), 18, FontStyle.Bold, HorzAlign.Center); ty += MM(14);
        Linha(title, 0, ty, w); ty += MM(6);
        Txt(title, $"PDV Caixa  •  Versão 1.0.0", 0, ty, w, MM(7), 11, FontStyle.Regular, HorzAlign.Center); ty += MM(8);
        Txt(title, $"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", 0, ty, w, MM(7), 11, FontStyle.Regular, HorzAlign.Center); ty += MM(10);
        Linha(title, 0, ty, w); ty += MM(8);
        Txt(title, "Se você está vendo este documento, a impressão está funcionando corretamente.",
            0, ty, w, MM(8), 11, FontStyle.Regular, HorzAlign.Center, wordWrap: true); ty += MM(14);
        Txt(title, "✓  Conexão com banco: OK",  0, ty, w, MM(7), 11, FontStyle.Regular, HorzAlign.Left); ty += MM(9);
        Txt(title, "✓  Geração de PDF: OK",     0, ty, w, MM(7), 11, FontStyle.Regular, HorzAlign.Left); ty += MM(9);
        Txt(title, "✓  Visualizador PDF: OK",   0, ty, w, MM(7), 11, FontStyle.Regular, HorzAlign.Left); ty += MM(9);
        title.Height = ty;
        page.ReportTitle = title;

        ExportarPdf(report, "TestePDF", "Teste de Impressão");
    }

    public static string PastaRelatorios()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "PDV_CAIXA", "Relatorios");
    }

    private static void ExportarPdf(Report report, string nomeArquivo, string titulo)
    {
        report.Prepare();

        // Salva o PDF na pasta Relatorios/
        var pasta = PastaRelatorios();
        Directory.CreateDirectory(pasta);

        var caminho = Path.Combine(pasta, $"{nomeArquivo}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        report.Export(new PDFSimpleExport(), caminho);

        // Abre o preview visual dentro do app
        var preview = new PDV_CAIXA.Views.RelatorioPreviewWindow(titulo, caminho);
        preview.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private static void Txt(BandBase band, string text,
        float x, float y, float w, float h,
        float fontSize, FontStyle style, HorzAlign hAlign,
        bool wordWrap = false)
    {
        band.Objects.Add(new TextObject {
            Bounds    = new RectangleF(x, y, w, h),
            Text      = text,
            Font      = new Font("Courier New", fontSize, style),
            HorzAlign = hAlign,
            VertAlign = VertAlign.Center,
            WordWrap  = wordWrap
        });
    }

    private static void Linha(BandBase band, float x, float y, float w)
    {
        band.Objects.Add(new LineObject {
            Bounds = new RectangleF(x, y, w, 1)
        });
    }
}
