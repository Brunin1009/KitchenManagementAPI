using Microsoft.EntityFrameworkCore;
using KitchenManagement.Models;

namespace KitchenManagement.Data
{
    public class KitchenContext : DbContext
    {
        public KitchenContext(DbContextOptions<KitchenContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
