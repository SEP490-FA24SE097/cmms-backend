using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class CartDTO
    {
        public string Id { get; set; }  
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string CustomerId { get; set; }
        public decimal Quantity { get; set; }
        public double TotalAmount { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class CartVM
    {
        public string Id { get; set; }
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string CustomerId { get; set; }
        public decimal Quantity { get; set; }
        public string ImageUrl { get; set; }    
        public double TotalAmount { get; set; }
    }

    public class AddItemDTO
    {
        public string CustomerId { get; set; }
        public string MaterialId  { get; set; }
        public string? VariantId { get; set; }
    }

    public class UpdateItemDTO
    {
        public string CustomerId { get; set; }
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string Quantity { get; set; }
    }

}
