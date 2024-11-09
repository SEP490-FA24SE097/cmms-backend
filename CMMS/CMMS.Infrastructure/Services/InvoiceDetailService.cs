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
    public interface IInvoiceDetailService
    {
        #region CURD
        Task<InvoiceDetail> FindAsync(string id);
        IQueryable<InvoiceDetail> GetAll();
        IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where);
        IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where, params Expression<Func<InvoiceDetail, object>>[] includes);
        IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where, Func<IQueryable<InvoiceDetail>, IIncludableQueryable<InvoiceDetail, object>> include = null);
        Task AddAsync(InvoiceDetail InvoiceDetail);
        Task AddRange(IEnumerable<InvoiceDetail> InvoiceDetails);
        void Update(InvoiceDetail InvoiceDetail);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<InvoiceDetail, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion
    }
    public class InvoiceDetailService : IInvoiceDetailService
    {
        private IInvoiceDetailRepository _invoiceDetailRepository;
        private IUnitOfWork _unitOfWork;

        public InvoiceDetailService(IInvoiceDetailRepository InvoiceDetailRepository,
            IUnitOfWork unitOfWork)
        {
            _invoiceDetailRepository = InvoiceDetailRepository;
            _unitOfWork = unitOfWork;
        }
        #region CURD 
        public async Task AddAsync(InvoiceDetail InvoiceDetail)
        {
            await _invoiceDetailRepository.AddAsync(InvoiceDetail);
        }

        public async Task AddRange(IEnumerable<InvoiceDetail> InvoiceDetails)
        {
            await _invoiceDetailRepository.AddRangce(InvoiceDetails);
        }

        public Task<bool> CheckExist(Expression<Func<InvoiceDetail, bool>> where)
        {
            return _invoiceDetailRepository.CheckExist(where);
        }

        public Task<InvoiceDetail> FindAsync(string id)
        {
            return _invoiceDetailRepository.FindAsync(id);
        }

        public IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where)
        {
            return _invoiceDetailRepository.Get(where);
        }

        public IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where, params Expression<Func<InvoiceDetail, object>>[] includes)
        {
            return _invoiceDetailRepository.Get(where, includes);
        }

        public IQueryable<InvoiceDetail> Get(Expression<Func<InvoiceDetail, bool>> where, Func<IQueryable<InvoiceDetail>, IIncludableQueryable<InvoiceDetail, object>> include = null)
        {
            return _invoiceDetailRepository.Get(where, include);
        }

        public IQueryable<InvoiceDetail> GetAll()
        {
            return _invoiceDetailRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _invoiceDetailRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(InvoiceDetail InvoiceDetail)
        {
            _invoiceDetailRepository.Update(InvoiceDetail);
        }
        #endregion
    }
}
