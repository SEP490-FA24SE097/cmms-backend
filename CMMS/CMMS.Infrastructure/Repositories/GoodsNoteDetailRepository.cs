using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IGoodsNoteDetailRepository : IBaseRepository<GoodsNoteDetail, Guid>
    {
        
    }

    public class GoodsNoteDetailRepository : BaseRepository<GoodsNoteDetail, Guid>, IGoodsNoteDetailRepository
    {
        public GoodsNoteDetailRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }

        
    }
}