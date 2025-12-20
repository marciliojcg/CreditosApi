using CreditosApi.Domain.Entities;
using CreditosApi.Domain.Interfaces;
using CreditosApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CreditosApi.Infrastructure.Repositories;

public class CreditoRepository : ICreditoRepository
{
    private readonly ApplicationDbContext _context;

    public CreditoRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Credito?> GetByIdAsync(long id)
    {
        return await _context.Creditos.FindAsync(id);
    }

    public async Task<IEnumerable<Credito>> GetAllAsync()
    {
        return await _context.Creditos.ToListAsync();
    }

    public async Task<Credito> AddAsync(Credito entity)
    {
        await _context.Creditos.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(Credito entity)
    {
        _context.Creditos.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Credito entity)
    {
        _context.Creditos.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<Credito?> GetByNumeroCreditoAsync(string numeroCredito)
    {
        return await _context.Creditos
            .FirstOrDefaultAsync(c => c.NumeroCredito == numeroCredito);
    }

    public async Task<IEnumerable<Credito>> GetByNumeroNfseAsync(string numeroNfse)
    {
        return await _context.Creditos
            .Where(c => c.NumeroNfse == numeroNfse)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNumeroCreditoAsync(string numeroCredito)
    {
        return await _context.Creditos
            .AnyAsync(c => c.NumeroCredito == numeroCredito);
    }
}