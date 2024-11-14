using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IConversionUnitRepository : IBaseRepository<ConversionUnit, Guid>
    {
        
    }

    public class ConversionUnitRepository : BaseRepository<ConversionUnit, Guid>, IConversionUnitRepository
    {
        public ConversionUnitRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}