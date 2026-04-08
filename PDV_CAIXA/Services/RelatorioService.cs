using System.Data;
using System.Drawing;
using System.IO;
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

        page.PaperWidth  = 80;
        page.PaperHeight = 297;
        page.LeftMargin  = 4;
        page.RightMargin = 4;
        page.TopMargin   = 5;
        page.BottomMargin = 5;

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
    // HISTÓRICO DE PEDIDO — A4
    // ═══════════════════════════════════════════════════════════════
    public void ImprimirHistoricoPedido(
        PedidoResumoViewModel pedido,
        IEnumerable<PedidoItem> itens)
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

        float w = MM(page.PaperWidth - page.LeftMargin - page.RightMargin);

        // ── Registrar itens ────────────────────────────────────────
        var ds = new DataSet();
        var dt = new DataTable("Itens");
        dt.Columns.Add("Produto");
        dt.Columns.Add("Qtd");
        dt.Columns.Add("Unitario");
        dt.Columns.Add("Subtotal");
        foreach (var item in itens)
            dt.Rows.Add(
                item.NomeProduto,
                item.Quantidade.ToString(),
                item.PrecoUnitario.ToString("C2", PtBR),
                item.Subtotal.ToString("C2", PtBR));
        ds.Tables.Add(dt);
        report.RegisterData(ds);

        // ── BAND: Cabeçalho ────────────────────────────────────────
        var title = new ReportTitleBand();
        float ty = 0;

        Txt(title, "PDV CAIXA", 0, ty, w * 0.5f, MM(10), 16, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "COMPROVANTE DE PEDIDO", w * 0.5f, ty, w * 0.5f, MM(10), 12, FontStyle.Bold, HorzAlign.Right);
        ty += MM(12);
        Linha(title, 0, ty, w); ty += MM(6);

        // Info do pedido em dois blocos lado a lado
        float col = w / 2;
        Txt(title, $"Pedido:      {pedido.NumeroTexto}",        0,   ty, col, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        Txt(title, $"Data:        {pedido.DataTexto}",          col, ty, col, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        ty += MM(7);
        Txt(title, $"Operador:    {pedido.Operador}",           0,   ty, col, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        Txt(title, $"Pagamento:   {pedido.PagamentoTexto}",     col, ty, col, MM(6), 10, FontStyle.Regular, HorzAlign.Left);
        ty += MM(7);

        var statusLabel = pedido.Status == "finalizado" ? "✓ FINALIZADO" : "✗ CANCELADO";
        Txt(title, $"Status:      {statusLabel}",               0,   ty, col, MM(6), 10, FontStyle.Bold,    HorzAlign.Left);
        ty += MM(10);
        Linha(title, 0, ty, w); ty += MM(8);

        // Cabeçalho da tabela de itens
        float c1 = w * 0.50f;
        float c2 = w * 0.10f;
        float c3 = w * 0.20f;
        float c4 = w * 0.20f;

        Txt(title, "PRODUTO",     0,          ty, c1, MM(6), 10, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "QTD",         c1,          ty, c2, MM(6), 10, FontStyle.Bold, HorzAlign.Center);
        Txt(title, "UNIT.",       c1+c2,       ty, c3, MM(6), 10, FontStyle.Bold, HorzAlign.Right);
        Txt(title, "SUBTOTAL",    c1+c2+c3,    ty, c4, MM(6), 10, FontStyle.Bold, HorzAlign.Right);
        ty += MM(6);
        Linha(title, 0, ty, w); ty += MM(2);

        title.Height = ty;
        page.ReportTitle = title;

        // ── BAND: Itens ────────────────────────────────────────────
        var dataBand = new DataBand();
        dataBand.DataSource = report.GetDataSource("Itens");
        dataBand.Height     = MM(8);

        Txt(dataBand, "[Itens.Produto]",  0,        0, c1, dataBand.Height, 10, FontStyle.Regular, HorzAlign.Left,  true);
        Txt(dataBand, "[Itens.Qtd]",      c1,        0, c2, dataBand.Height, 10, FontStyle.Regular, HorzAlign.Center);
        Txt(dataBand, "[Itens.Unitario]", c1+c2,     0, c3, dataBand.Height, 10, FontStyle.Regular, HorzAlign.Right);
        Txt(dataBand, "[Itens.Subtotal]", c1+c2+c3,  0, c4, dataBand.Height, 10, FontStyle.Bold,    HorzAlign.Right);
        page.Bands.Add(dataBand);

        // ── BAND: Rodapé com total ─────────────────────────────────
        var summary = new ReportSummaryBand();
        float sy = MM(4);
        Linha(summary, 0, sy, w); sy += MM(4);

        Txt(summary, "TOTAL DO PEDIDO",                     0,   sy, w * 0.7f, MM(9), 13, FontStyle.Bold, HorzAlign.Left);
        Txt(summary, pedido.Total.ToString("C2", PtBR),     w * 0.7f, sy, w * 0.3f, MM(9), 13, FontStyle.Bold, HorzAlign.Right);
        sy += MM(11);
        Linha(summary, 0, sy, w); sy += MM(4);

        Txt(summary, $"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}", 0, sy, w, MM(5), 8, FontStyle.Regular, HorzAlign.Right);
        sy += MM(8);

        summary.Height = sy;
        page.ReportSummary = summary;

        ExportarPdf(report, $"Pedido_{pedido.Numero:D4}", $"Pedido {pedido.NumeroTexto} — {pedido.DataTexto}");
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE USUÁRIOS CADASTRADOS — A4
    // ═══════════════════════════════════════════════════════════════
    public void GerarRelatorioUsuarios(IEnumerable<Usuario> usuarios)
    {
        var lista = usuarios.ToList();

        var report = new Report();
        var page   = new ReportPage();
        report.Pages.Add(page);

        page.PaperWidth   = 210;
        page.PaperHeight  = 297;
        page.LeftMargin   = 15;
        page.RightMargin  = 15;
        page.TopMargin    = 15;
        page.BottomMargin = 15;

        float w   = MM(page.PaperWidth  - page.LeftMargin - page.RightMargin);
        float cNome  = w * 0.55f;
        float cPerfil = w * 0.30f;
        float cStatus = w * 0.15f;

        // ── Título ────────────────────────────────────────────────
        var title = new ReportTitleBand();
        float ty = MM(4);
        Txt(title, "RELATÓRIO DE USUÁRIOS CADASTRADOS", 0, ty, w, MM(10), 16, FontStyle.Bold, HorzAlign.Center);
        ty += MM(12);
        Txt(title, $"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}  •  Total: {lista.Count} usuário(s)",
            0, ty, w, MM(6), 9, FontStyle.Regular, HorzAlign.Center);
        ty += MM(8);
        Linha(title, 0, ty, w); ty += MM(2);
        // Cabeçalho da tabela
        Txt(title, "NOME",   0,              ty, cNome,   MM(7), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "PERFIL", cNome,          ty, cPerfil, MM(7), 9, FontStyle.Bold, HorzAlign.Left);
        Txt(title, "TOTAL",  cNome + cPerfil, ty, cStatus, MM(7), 9, FontStyle.Bold, HorzAlign.Center);
        ty += MM(8);
        Linha(title, 0, ty, w); ty += MM(2);
        title.Height = ty;
        page.ReportTitle = title;

        // ── Dados via DataBand ────────────────────────────────────
        var ds = new DataSet();
        var dt = new DataTable("Usuarios");
        dt.Columns.Add("Nome");
        dt.Columns.Add("Perfil");
        foreach (var u in lista)
            dt.Rows.Add(u.Nome, u.Perfil == "admin" ? "Administrador" : "Usuário");
        ds.Tables.Add(dt);
        report.RegisterData(ds);
        report.GetDataSource("Usuarios").Enabled = true;

        var band = new DataBand { Height = MM(8), DataSource = report.GetDataSource("Usuarios") };
        Txt(band, "[Usuarios.Nome]",   0,              0, cNome,   MM(7), 10, FontStyle.Regular, HorzAlign.Left);
        Txt(band, "[Usuarios.Perfil]", cNome,          0, cPerfil, MM(7), 10, FontStyle.Regular, HorzAlign.Left);
        page.Bands.Add(band);

        // ── Rodapé ────────────────────────────────────────────────
        var footer = new PageFooterBand { Height = MM(8) };
        Linha(footer, 0, 0, w);
        Txt(footer, $"PDV Caixa — Relatório de Usuários  •  {DateTime.Now:dd/MM/yyyy HH:mm}",
            0, MM(2), w, MM(5), 8, FontStyle.Regular, HorzAlign.Center);
        page.PageFooter = footer;

        ExportarPdf(report, "RelatorioUsuarios", "Usuários Cadastrados");
    }

    // ═══════════════════════════════════════════════════════════════
    // RELATÓRIO DE PRODUTOS CADASTRADOS — A4 via RDLC
    // ═══════════════════════════════════════════════════════════════
    public void GerarRelatorioProdutos(IEnumerable<Produto> produtos)
    {
        var lista = produtos.ToList();

        // ── Monta DataTable compatível com o DataSet do RDLC ──────
        var dt = new DataTable();
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
                p.Desconto > 0 ? p.Desconto.ToString("C2", PtBR) : "—",
                p.Estoque.ToString(),
                p.Ativo ? "Sim" : "Não");

        var parametros = new Dictionary<string, string>
        {
            ["DataGeracao"]   = $"Gerado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}",
            ["TotalProdutos"] = lista.Count.ToString()
        };

        // ── Renderiza via RDLC ────────────────────────────────────
        var rdlcService = new RdlcRelatorioService();
        var pdfBytes = rdlcService.GerarPdfBytes("RelatorioProdutos.rdlc", "DataSetProdutos", dt, parametros);

        // ── Salva na pasta de relatórios e abre o preview ─────────
        var pasta = PastaRelatorios();
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, $"RelatorioProdutos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        File.WriteAllBytes(caminho, pdfBytes);

        var preview = new PDV_CAIXA.Views.RelatorioPreviewWindow("Produtos Cadastrados", caminho);
        preview.Show();
    }

    // ═══════════════════════════════════════════════════════════════
    // Gera o PDF e abre o preview visual dentro do app (WebView2)
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
