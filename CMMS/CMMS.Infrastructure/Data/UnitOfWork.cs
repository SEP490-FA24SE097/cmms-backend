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
    }
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _applicationDbContext;
        public UnitOfWork(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task<bool> SaveChangeAsync()
        {
            return (await _applicationDbContext.SaveChangesAsync()) > 0;
        }
    }
}
