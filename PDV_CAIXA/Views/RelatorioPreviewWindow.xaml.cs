using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace PDV_CAIXA.Views;

public partial class RelatorioPreviewWindow : Window
{
    private readonly string _caminhoPdf;

    public RelatorioPreviewWindow(string titulo, string caminhoPdf)
    {
        InitializeComponent();
        _caminhoPdf       = caminhoPdf;
        txtTituloRelatorio.Text = titulo;
        Title             = $"Relatório — {titulo}";
        InicializarWebView();
    }

    private async void InicializarWebView()
    {
        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.Navigate(new Uri(_caminhoPdf).AbsoluteUri);
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        webView.CoreWebView2.ExecuteScriptAsync("window.print()");
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
        => Close();
}
