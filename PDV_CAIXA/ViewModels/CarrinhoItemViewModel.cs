using System.ComponentModel;
using System.Globalization;

namespace PDV_CAIXA.ViewModels {
    public class CarrinhoItemViewModel : INotifyPropertyChanged {
        private int _quantidade = 1;

        public Guid    ProdutoId     { get; set; }
        public string  Nome          { get; set; } = string.Empty;
        public decimal PrecoUnitario { get; set; }

        public int Quantidade {
            get => _quantidade;
            set {
                if (value < 1) return;
                _quantidade = value;
                OnPropertyChanged(nameof(Quantidade));
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(SubtotalTexto));
            }
        }

        public decimal Subtotal      => PrecoUnitario * Quantidade;
        public string  PrecoTexto    => PrecoUnitario.ToString("C2", new CultureInfo("pt-BR"));
        public string  SubtotalTexto => Subtotal.ToString("C2", new CultureInfo("pt-BR"));

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
