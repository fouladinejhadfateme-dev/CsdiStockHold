using StockHold.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Application.Abstractions.Persistence.Customers
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int customerId); 
    }
}
