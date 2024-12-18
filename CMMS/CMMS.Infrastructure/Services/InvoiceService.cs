using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Helpers;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public class StoreRevenue
    {
        public int? Month { get; set; }
        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public int TotalInvoices { get; set; }  // Số lượng hóa đơn đã hoàn tất
        public decimal TotalRevenue { get; set; }  // Tổng doanh thu
        public decimal TotalRefunds { get; set; }  // Tổng hoàn tiền
        public decimal Profit { get; set; }  // Lợi nhuận
    }
    public interface IInvoiceService
    {
        #region CURD
        Task<Invoice> FindAsync(string id);
        IQueryable<Invoice> GetAll();
        IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where);
        IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where, params Expression<Func<Invoice, object>>[] includes);
        IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where, Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>> include = null);
        Task AddAsync(Invoice Invoice);
        Task AddRange(IEnumerable<Invoice> Invoices);
        void Update(Invoice Invoice);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<Invoice, bool>> where);
        Task<bool> SaveChangeAsync();
        string GenerateInvoiceCode();
        #endregion

        Task<List<StoreRevenue>> GetStoreRevenueAsync(DashboardInvoiceFitlerModel filterModel);
        Task<List<StoreRevenue>> GetMonthlyRevenueAsync(DashboardInvoiceFitlerModel filterModel);
    }
    public class InvoiceService : IInvoiceService
    {
        private IInvoiceRepository _invoiceRepository;
        private IUnitOfWork _unitOfWork;
        private IStoreService _storeService;

        public InvoiceService(IInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork, IStoreService storeService)
        {
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
            _storeService = storeService;
        }
        #region CURD 
        public async Task AddAsync(Invoice Invoice)
        {
            await _invoiceRepository.AddAsync(Invoice);
        }

        public async Task AddRange(IEnumerable<Invoice> Invoices)
        {
            await _invoiceRepository.AddRangce(Invoices);
        }

        public Task<bool> CheckExist(Expression<Func<Invoice, bool>> where)
        {
            return _invoiceRepository.CheckExist(where);
        }

        public Task<Invoice> FindAsync(string id)
        {
            return _invoiceRepository.FindAsync(id);
        }



        public IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where)
        {
            return _invoiceRepository.Get(where);
        }

        public IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where, params Expression<Func<Invoice, object>>[] includes)
        {
            return _invoiceRepository.Get(where, includes);
        }

        public IQueryable<Invoice> Get(Expression<Func<Invoice, bool>> where, Func<IQueryable<Invoice>, IIncludableQueryable<Invoice, object>> include = null)
        {
            return _invoiceRepository.Get(where, include);
        }

        public IQueryable<Invoice> GetAll()
        {
            return _invoiceRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _invoiceRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Invoice Invoice)
        {
            _invoiceRepository.Update(Invoice);
        }
        #endregion

        public string GenerateInvoiceCode()
        {
            var invoiceTotal = _invoiceRepository.GetAll();
            string invoiceCode = $"HD{(invoiceTotal.Count() + 1):D6}";
            return invoiceCode;
        }

        public async Task<List<StoreRevenue>> GetStoreRevenueAsync(DashboardInvoiceFitlerModel filterModel)
        {
            // Lấy danh sách tất cả các cửa hàng
            var allStores = _storeService.GetAll(); 
            var storeRevenuesList = new List<StoreRevenue>();

            // Lọc hóa đơn theo các điều kiện
            var invoices = _invoiceRepository
                .Get(_ => _.Id != null &&
                          (!filterModel.SpecificDate.HasValue ||
                           (_.InvoiceDate <= ((DateTime)filterModel.SpecificDate).Date.AddDays(1).AddMilliseconds(-1) &&
                            _.InvoiceDate >= ((DateTime)filterModel.SpecificDate).Date)) &&
                          (filterModel.Year == null || _.InvoiceDate.Year.Equals(filterModel.Year)))
                .GroupBy(i => i.StoreId)
                .ToList(); // Chuyển thành danh sách để dễ xử lý

            // Kết hợp dữ liệu từ danh sách tất cả các cửa hàng và hóa đơn đã lọc
            foreach (var store in allStores)
            {
                var invoiceGroup = invoices.FirstOrDefault(g => g.Key == store.Id);

                var storeRevenue = new StoreRevenue
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    TotalInvoices = invoiceGroup?.Count(i => i.InvoiceStatus == (int)InvoiceStatus.Done) ?? 0,
                    TotalRevenue = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Done).Sum(i => i.SalePrice ?? 0) ?? 0,
                    TotalRefunds = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Refund).Sum(i => i.SalePrice ?? 0) ?? 0,
                    Profit = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Done).Sum(i => (i.SalePrice ?? 0) - (i.Discount ?? 0)) ?? 0
                };

                storeRevenuesList.Add(storeRevenue);
            }

            return storeRevenuesList;
        }


        public async Task<List<StoreRevenue>> GetMonthlyRevenueAsync(DashboardInvoiceFitlerModel fitlerModel)
        {
            var monthlyRevenues = new List<StoreRevenue>();

            // Lọc hóa đơn theo năm
            var invoices = _invoiceRepository
                .Get(_ => _.InvoiceDate.Year == fitlerModel.Year && _.StoreId.EndsWith(fitlerModel.StoreId))
                .GroupBy(i => i.InvoiceDate.Month)
                .ToList();

            // Tạo dữ liệu doanh thu theo từng tháng
            for (int month = 1; month <= 12; month++)
            {
                var invoiceGroup = invoices.FirstOrDefault(g => g.Key == month);
                var store = _storeService.Get(_ => _.Id.Equals(fitlerModel.StoreId)).FirstOrDefault();

                var monthlyRevenue = new StoreRevenue
                {
                    Month = month,
                    StoreId = store.Id,
                    StoreName = store.Name,
                    TotalInvoices = invoiceGroup?.Count(i => i.InvoiceStatus == (int)InvoiceStatus.Done) ?? 0,
                    TotalRevenue = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Done).Sum(i => i.SalePrice ?? 0) ?? 0,
                    TotalRefunds = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Refund).Sum(i => i.SalePrice ?? 0) ?? 0,
                    Profit = invoiceGroup?.Where(i => i.InvoiceStatus == (int)InvoiceStatus.Done).Sum(i => (i.SalePrice ?? 0) - (i.Discount ?? 0)) ?? 0
                };
                monthlyRevenues.Add(monthlyRevenue);
            }
            return monthlyRevenues;
        }
    }
}
