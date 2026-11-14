using StockHold.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockHold.Domain.Entities
{
    public class Customer
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public CustomerType CustomerType { get; private set; }


        private Customer() { }
        public Customer(string name, CustomerType customerType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("نام مشتری نمی‌تواند خالی باشد.");

            if (!Enum.IsDefined(typeof(CustomerType), customerType))
                throw new ArgumentException("نوع مشتری معتبر نیست.");

            Name = name;
            CustomerType = customerType;
        }
        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("نام مشتری نمی‌تواند خالی باشد.");

            Name = newName;
        }
        public int GetMaximumReservationQuantity()
        {
            return CustomerType switch
            {
                CustomerType.Normal => 3,
                CustomerType.Special => 10,
                _ => throw new InvalidOperationException("نوع مشتری معتبر نیست.")
            };
        }
        public TimeSpan GetReservationValidity()
        {
            return CustomerType switch
            {
                //=> Switch Expression 
                CustomerType.Normal => TimeSpan.FromMinutes(10),
                CustomerType.Special => TimeSpan.FromMinutes(30),
                _ => throw new InvalidOperationException("نوع مشتری معتبر نیست.")
            };
        }

    }
}
