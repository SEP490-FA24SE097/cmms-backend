using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Helpers;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
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

        Task<List<MonthlyRevenueDto>> GetStoreMonthlyRevenueAsync();
    }
    public class InvoiceService : IInvoiceService
    {
        private IInvoiceRepository _invoiceRepository;
        private IUnitOfWork _unitOfWork;
        private IStoreService _storeService;
        private IStoreInventoryService _storeInventoryService;
        private IMaterialService _materialService;
        private IVariantService _variantService;

        public InvoiceService(IInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork, IStoreService storeService,
            IStoreInventoryService storeInventoryService,
            IMaterialService materialService,
            IVariantService variantService)
        {
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
            _storeService = storeService;
            _storeInventoryService = storeInventoryService;
            _materialService = materialService;
            _variantService = variantService;
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

        public async Task<List<MonthlyRevenueDto>> GetStoreMonthlyRevenueAsync()
        {
            var stores = _invoiceRepository.Get(_ => _.InvoiceStatus.Equals((int)InvoiceStatus.Done) ||
            _.InvoiceStatus.Equals((int)InvoiceStatus.DoneInStore)).GroupBy(_ => new
            {
                Year = _.InvoiceDate.Year,
                Month = _.InvoiceDate.Month,
                _.StoreId
            }).Select(g => new MonthlyRevenueDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                StoreId = g.Key.StoreId,
                MonthlyRevenue = g.Sum(i => i.TotalAmount)
            })
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .ThenBy(r => r.StoreId);

            return await stores.ToListAsync();
        }


        //public async Task<StoreSummaryData> GetStoreRevuenueAsync(string year, string storeId)
        //{
        //    // Lấy dữ liệu tổng hợp từ cơ sở dữ liệu
         
        //}


    }
}
