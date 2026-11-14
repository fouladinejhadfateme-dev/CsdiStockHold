using Microsoft.EntityFrameworkCore;
using StockHold.Application.Abstractions.Persistence.Customers;
using StockHold.Domain.Entities;
using StockHold.Infrastructure.Persistence.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Infrastructure.Persistence.Repositories.Customers
{
    public class CustomerRepository:ICustomerRepository
    {
        private readonly StockHoldDbContext _context;

        public CustomerRepository(StockHoldDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(int customerId)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId);
        }
    }
}
