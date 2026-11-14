
using Microsoft.EntityFrameworkCore.Storage;
using StockHold.Application.Abstractions.Persistence;
using StockHold.Infrastructure.Persistence.Contexts;

namespace StockHold.Infrastructure.Persistence.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly StockHoldDbContext _context;

    private IDbContextTransaction? _transaction;

    public UnitOfWork(StockHoldDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "یک تراکنش از قبل شروع شده است.");

        _transaction =
            await _context.Database.BeginTransactionAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is null)
            return;

        var transaction = _transaction;
        _transaction = null;

        try
        {
            await transaction.CommitAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is null)
            return;

        var transaction = _transaction;
        _transaction = null;

        try
        {
            await transaction.RollbackAsync();
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }
}
