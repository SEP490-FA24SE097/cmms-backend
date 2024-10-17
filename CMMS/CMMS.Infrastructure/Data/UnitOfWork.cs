using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Data
{
    public interface IUnitOfWork
    {
        Task<bool> SaveChangeAsync();
		void BeginTransaction();
		void Commit();
		void Rollback();
	}
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _applicationDbContext;
		private IDbContextTransaction _transaction;
		public UnitOfWork(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

		public void BeginTransaction()
		{
			_transaction = _applicationDbContext.Database.BeginTransaction();
		}

		public void Commit()
		{
			_transaction?.Commit();
			_transaction?.Dispose();
			_transaction = null;
		}

		public void Rollback()
		{
			_transaction?.Rollback();
			_transaction?.Dispose();
			_transaction = null;
		}

		public async Task<bool> SaveChangeAsync()
        {
            return (await _applicationDbContext.SaveChangesAsync()) > 0;
        }
    }
}
