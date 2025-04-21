using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("orders")]
    public class Order
    {
        public Order()
        {
            Cliente = string.Empty;
            Produto = string.Empty;
            Status = "Pendente";
            DataCriacao = DateTime.UtcNow;
        }

        [Column("id")]
        public Guid Id { get; set; }

        [Column("cliente")]
        public string Cliente { get; set; }

        [Column("produto")]
        public string Produto { get; set; }

        [Column("valor")]
        public decimal Valor { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("data_criacao")]
        public DateTime DataCriacao { get; set; }
    }
} 