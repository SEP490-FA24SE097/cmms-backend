using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    }
    public class InvoiceService : IInvoiceService
    {
        private IInvoiceRepository _invoiceRepository;
        private IUnitOfWork _unitOfWork;

        public InvoiceService(IInvoiceRepository invoiceRepository, 
            IUnitOfWork unitOfWork)
        {
            _invoiceRepository = invoiceRepository;
            _unitOfWork = unitOfWork;
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
    }
}
