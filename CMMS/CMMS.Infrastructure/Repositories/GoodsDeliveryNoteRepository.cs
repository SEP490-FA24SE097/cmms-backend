using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IGoodsDeliveryNoteRepository : IBaseRepository<GoodsDeliveryNote, Guid>
    {

    }

    public class GoodsDeliveryNoteRepository : BaseRepository<GoodsDeliveryNote, Guid>, IGoodsDeliveryNoteRepository
    {
        public GoodsDeliveryNoteRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }


    }
}