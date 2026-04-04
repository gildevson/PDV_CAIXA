using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;
using PDV_CAIXA.ViewModels;

namespace PDV_CAIXA.Services {
    public class CaixaService {
        private readonly CaixaRepository _repo = new();

        public Caixa? ObterCaixaAberto() => _repo.ObterAberto();

        public Caixa AbrirCaixa(decimal saldoInicial, Guid usuarioId)
            => _repo.Abrir(saldoInicial, usuarioId);

        public void FecharCaixa(Guid caixaId)
            => _repo.Fechar(caixaId);

        public void RegistrarEntrada(Guid caixaId, string descricao, decimal valor,
                                     Guid? pedidoId = null, string origem = "manual") {
            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId   = caixaId,
                Tipo      = "entrada",
                Descricao = descricao,
                Valor     = valor,
                Origem    = origem,
                PedidoId  = pedidoId
            });
        }

        public void RegistrarSaida(Guid caixaId, string descricao, decimal valor) {
            _repo.InserirMovimentacao(new MovimentacaoCaixa {
                CaixaId   = caixaId,
                Tipo      = "saida",
                Descricao = descricao,
                Valor     = valor,
                Origem    = "manual"
            });
        }

        public IEnumerable<MovimentacaoCaixa> ListarMovimentacoes(Guid caixaId)
            => _repo.ListarMovimentacoes(caixaId);

        public IEnumerable<CaixaSessaoViewModel> ListarHistorico() {
            return _repo.ListarHistoricoDetalhado().Select(t => {
                var (entradas, saidas) = _repo.ObterTotais(t.caixa.Id);
                return new CaixaSessaoViewModel {
                    Id             = t.caixa.Id,
                    DataAbertura   = t.caixa.DataAbertura,
                    DataFechamento = t.caixa.DataFechamento,
                    SaldoInicial   = t.caixa.SaldoInicial,
                    TotalEntradas  = entradas,
                    TotalSaidas    = saidas,
                    SaldoFinal     = t.caixa.SaldoInicial + entradas - saidas,
                    Status         = t.caixa.Status,
                    NomeOperador   = t.nomeOperador
                };
            });
        }

        public (decimal saldoAtual, decimal entradas, decimal saidas) ObterResumo(Guid caixaId) {
            var caixa = _repo.ObterAberto() ?? throw new InvalidOperationException("Caixa não encontrado.");
            var (entradas, saidas) = _repo.ObterTotais(caixaId);
            var saldoAtual = caixa.SaldoInicial + entradas - saidas;
            return (saldoAtual, entradas, saidas);
        }
    }
}
