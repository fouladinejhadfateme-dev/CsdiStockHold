
namespace StockHold.Domain.Entities
{
    public class Product
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Quantity { get; private set; }

        // فقط برای EF Core
        private Product() { }

        // برای ساخت کالای جدید
        public Product(string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("نام کالا نمی‌تواند خالی باشد.");

            if (quantity < 0)
                throw new ArgumentException("موجودی کالا نمی‌تواند منفی باشد.");

            Name = name;
            Quantity = quantity;
        }

        public void UpdateName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("نام کالا نمی‌تواند خالی باشد.");

            Name = newName;
        }

        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("تعداد افزایش موجودی باید بیشتر از صفر باشد.");

            Quantity += quantity;
        }
    }
}
