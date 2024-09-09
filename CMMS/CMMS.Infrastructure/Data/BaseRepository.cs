using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Data
{
    public interface IBaseRepository<T, TKey> where T : class
    {
        Task<T> FindAsync(TKey id);
        IQueryable<T> GetAll();
        IQueryable<T> Get(Expression<Func<T, bool>> where);
        IQueryable<T> Get(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes);
        IQueryable<T> Get(Expression<Func<T, bool>> where, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);
        Task AddAsync(T entity);
        Task AddRangce(IEnumerable<T> entities);
        void Update(T entity);
        Task<bool> Remove(TKey id);
        void Remove(T entity);
        Task Remove(Expression<Func<T, bool>> where);
        void Remove(IEnumerable<T> list);
        object Max(Expression<Func<T, object>> selector);
        Task<bool> CheckExist(Expression<Func<T, bool>> where);
    }
    public class BaseRepository<T, Tkey> : IBaseRepository<T, Tkey> where T : class
    {
        private ApplicationDbContext _applicationDbContext;
        private DbSet<T> dbSet;
        public BaseRepository(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
            dbSet = _applicationDbContext.Set<T>();
        }
        public virtual async Task AddAsync(T entity)
        {
            await dbSet.AddAsync(entity);
        }

        public virtual async Task AddRangce(IEnumerable<T> entities)
        {
            await dbSet.AddRangeAsync(entities);
        }

        public virtual async Task<T> FindAsync(Tkey id)
        {
            return await dbSet.FindAsync(id);
        }

        public virtual IQueryable<T> GetAll()
        {
            return dbSet;
        }

        public virtual IQueryable<T> Get(Expression<Func<T, bool>> where)
        {
            return dbSet.Where(where);
        }

        public virtual IQueryable<T> Get(Expression<Func<T, bool>> where, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> result = dbSet;
            if (where != null)
            {
                result = dbSet.Where(where);
            }
            foreach (var include in includes)
            {
                result = result.Include(include);
            }
            return result;
        }

        public virtual IQueryable<T> Get(Expression<Func<T, bool>> where, Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
        {
            IQueryable<T> result = dbSet;
            if (where != null)
            {
                result = dbSet.Where(where);
            }

            if (include != null)
            {
                result = include(result);
            }
            return result;
        }

        public virtual async Task<bool> Remove(Tkey id)
        {
            T entity = await dbSet.FindAsync(id);
            if (entity == null)
            {
                return false;
            }
            dbSet.Remove(entity);
            return true;
        }

        public virtual void Update(T entity)
        {
            _applicationDbContext.Entry<T>(entity).State = EntityState.Modified;
        }

        public async Task<bool> CheckExist(Expression<Func<T, bool>> where)
        {
            return await dbSet.AnyAsync(where);
        }

        public async Task Remove(Expression<Func<T, bool>> where)
        {
            var entities = dbSet.Where(where);
            dbSet.RemoveRange(entities);
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public object Max(Expression<Func<T, object>> selector)
        {
            return dbSet.Max(selector);
        }

        public void Remove(IEnumerable<T> list)
        {
            dbSet.RemoveRange(list);
        }
    }
}
