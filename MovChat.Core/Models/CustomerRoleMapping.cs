using System.ComponentModel.DataAnnotations.Schema;

namespace MovChat.Core.Models
{
    [Table("Customer_CustomerRole_Mapping")]
    public class CustomerRoleMapping
    {
        [Column("Customer_Id")]
        public int CustomerId { get; set; }

        [Column("CustomerRole_Id")]
        public int CustomerRoleId { get; set; }
    }
}
