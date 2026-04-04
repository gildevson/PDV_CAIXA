using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Services {
    public class PedidoService {
        private readonly PedidoRepository _repo = new();

        public Pedido Finalizar(Guid usuarioId, IEnumerable<PedidoItem> itens, string formaPagamento) {
            var lista  = itens.ToList();
            var pedido = new Pedido {
                Id             = Guid.NewGuid(),
                Data           = DateTime.Now,
                Total          = lista.Sum(i => i.Subtotal),
                UsuarioId      = usuarioId,
                Status         = "finalizado",
                FormaPagamento = formaPagamento
            };

            foreach (var item in lista) {
                item.Id       = Guid.NewGuid();
                item.PedidoId = pedido.Id;
            }

            _repo.Salvar(pedido, lista);
            return pedido;
        }
    }
}
