﻿using CMMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Core.Models
{
    public class CustomerBalanceDTO
    {
        public double TotalDebt { get; set; }
        public double TotalPaid { get; set; } 
        public double Balance { get; set; }
        public string CustomerId { get; set; }
        public string? Note { get; set; }
    }
    public class CustomerBalanceUpdateModel 
    {
        public string Id { get; set; }
        public double TotalDebt { get; set; }
        public double TotalPaid { get; set; }
        public double Balance { get; set; }
        public string? CustomerId { get; set; }
        public string? Note { get; set; }
    }

    public class CustomerBalanceVM
    {
        public string Id { get; set; }
        public double TotalDebt { get; set; }
        public double TotalPaid { get; set; }
        public double Balance { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserVM UserVM { get; set; }  
    }

    public class CustomerBalanceFitlerModel
    {
        public string? CustomerName { get; set; }
        public DefaultSearch defaultSearch  { get; set; }
        public CustomerBalanceFitlerModel()
        {
            defaultSearch = new DefaultSearch();
        }
    }
}
