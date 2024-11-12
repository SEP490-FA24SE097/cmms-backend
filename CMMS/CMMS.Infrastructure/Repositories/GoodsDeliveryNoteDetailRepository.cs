using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IGoodsDeliveryNoteDetailRepository : IBaseRepository<GoodsDeliveryNoteDetail, Guid>
    {
        
    }

    public class GoodsDeliveryNoteDetailRepository : BaseRepository<GoodsDeliveryNoteDetail, Guid>, IGoodsDeliveryNoteDetailRepository
    {
        public GoodsDeliveryNoteDetailRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }

        
    }
}