namespace Models
{
    public class OrderDto
    {
        public string Cliente { get; set; } = string.Empty;
        public string Produto { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }
} 