namespace CreditosApi.Domain.Interfaces;

public interface ICreditoRepository : IRepository<Entities.Credito>
{
    Task<Entities.Credito?> GetByNumeroCreditoAsync(string numeroCredito);
    Task<IEnumerable<Entities.Credito>> GetByNumeroNfseAsync(string numeroNfse);
    Task<bool> ExistsByNumeroCreditoAsync(string numeroCredito);
}