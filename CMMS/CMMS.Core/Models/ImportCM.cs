﻿using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class ImportCM
    {
        public Guid? SupplierId { get; set; }
        public string? StoreId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalDiscount { get; set; }
        public bool IsCompleted { get; set; }
        public string? Note { get; set; }
        public decimal TotalDue { get; set; }
        public virtual ICollection<ImportDetailCM> ImportDetails { get; set; }
    }
    public class ImportDetailCM
    {
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitDiscount { get; set; }
        public decimal PriceAfterDiscount { get; set; }
        public string? Note { get; set; }
    }
    public class ImportDetailUM
    {
        public Guid? Id { get; set; } 
        public Guid MaterialId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitDiscount { get; set; }
        public decimal PriceAfterDiscount { get; set; }
        public string? Note { get; set; }
    }
    public class ImportUM
    {
        public Guid ImportId { get; set; }
        public Guid? SupplierId { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalDiscount { get; set; }
        public string Status { get; set; }
        public string? Note { get; set; }
        public decimal TotalDue { get; set; }
        public virtual ICollection<ImportDetailUM> ImportDetails { get; set; }
    }
}
