using PDV_CAIXA.Models;
using PDV_CAIXA.Repositories;

namespace PDV_CAIXA.Services {
    public class ProdutoService {
        private readonly ProdutoRepository _repo = new();

        public IEnumerable<Produto> ObterTodos() => _repo.ObterTodos();

        public Produto? ObterPorId(Guid id) => _repo.ObterPorId(id);

        public Produto? ObterPorCodigoBarras(string codigo) => _repo.ObterPorCodigoBarras(codigo);

        public void Inserir(Produto produto) => _repo.Inserir(produto);

        public void Atualizar(Produto produto) => _repo.Atualizar(produto);

        public void Excluir(Guid id) => _repo.Excluir(id);
    }
}
