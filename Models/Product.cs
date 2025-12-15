using System.ComponentModel.DataAnnotations;

namespace KitchenManagement.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Category { get; set; } = string.Empty;
        
        public int Quantity { get; set; }
        
        public DateTime? ExpirationDate { get; set; }
    }
}
