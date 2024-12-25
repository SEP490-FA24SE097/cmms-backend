using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IGoodsNoteRepository : IBaseRepository<GoodsNote, Guid>
    {

    }

    public class GoodsNoteRepository : BaseRepository<GoodsNote, Guid>, IGoodsNoteRepository
    {
        public GoodsNoteRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }


    }
}