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
    public interface ITransactionService
    {
        #region CURD
        Task<Transaction> FindAsync(string id);
        IQueryable<Transaction> GetAll();
        IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where);
        IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where, params Expression<Func<Transaction, object>>[] includes);
        IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where, Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>> include = null);
        Task AddAsync(Transaction Transaction);
        Task AddRange(IEnumerable<Transaction> Transactions);
        void Update(Transaction Transaction);
        Task<bool> Remove(string id);
        Task<bool> CheckExist(Expression<Func<Transaction, bool>> where);
        Task<bool> SaveChangeAsync();
        #endregion

        string GenerateTransactionCode(string prefix);
    }
    public class TransactionService : ITransactionService
    {
        private ITransactionRepository _transactionRepository;
        private IUnitOfWork _unitOfWork;

        public TransactionService(ITransactionRepository transactionRepository,
            IUnitOfWork unitOfWork)
        {
            _transactionRepository = transactionRepository;
            _unitOfWork = unitOfWork;
        }
        #region CURD 
        public async Task AddAsync(Transaction Transaction)
        {
            await _transactionRepository.AddAsync(Transaction);
        }

        public async Task AddRange(IEnumerable<Transaction> Transactions)
        {
            await _transactionRepository.AddRangce(Transactions);
        }

        public Task<bool> CheckExist(Expression<Func<Transaction, bool>> where)
        {
            return _transactionRepository.CheckExist(where);
        }

        public Task<Transaction> FindAsync(string id)
        {
            return _transactionRepository.FindAsync(id);
        }


        public IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where)
        {
            return _transactionRepository.Get(where);
        }

        public IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where, params Expression<Func<Transaction, object>>[] includes)
        {
            return _transactionRepository.Get(where, includes);
        }

        public IQueryable<Transaction> Get(Expression<Func<Transaction, bool>> where, Func<IQueryable<Transaction>, IIncludableQueryable<Transaction, object>> include = null)
        {
            return _transactionRepository.Get(where, include);
        }

        public IQueryable<Transaction> GetAll()
        {
            return _transactionRepository.GetAll();
        }

        public async Task<bool> Remove(string id)
        {
            return await _transactionRepository.Remove(id);
        }

        public async Task<bool> SaveChangeAsync()
        {
            return await _unitOfWork.SaveChangeAsync();
        }

        public void Update(Transaction Transaction)
        {
            _transactionRepository.Update(Transaction);
        }
        #endregion


        public string GenerateTransactionCode(string prefix)
        {
            var transactionTotal = _transactionRepository.Get(_ => _.Id.Substring(0, 2).Contains(prefix));
            string invoiceCode = $"{prefix}{(transactionTotal.Count() + 1):D6}";
            return invoiceCode;
        }
    }
}
