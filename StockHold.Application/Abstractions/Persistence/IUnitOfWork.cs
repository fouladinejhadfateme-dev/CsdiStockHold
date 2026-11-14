using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Application.Abstractions.Persistence
{
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync();

        Task SaveChangesAsync();

        Task CommitTransactionAsync();

        Task RollbackTransactionAsync();
    }
}
