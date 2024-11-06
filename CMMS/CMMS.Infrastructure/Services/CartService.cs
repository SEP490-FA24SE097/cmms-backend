using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface ICartService
    {
        Task<StoreInventory> GetItemInStoreAsync(AddItemModel itemModel);

    }
    public class CartService : ICartService
    {
        private IStoreInventoryService _storeInventoryService;
        private IMapper _mapper;
        private IUnitOfWork _unitOfWork;
        private IUserService _userService;
        private IMaterialService _materialService;
        private IVariantService _variantService;
        private static Dictionary<string, List<CartItem>> userCarts = new Dictionary<string, List<CartItem>>();
        public CartService(IUnitOfWork unitOfWork,
            IUserService userService, IMapper mapper,
            IMaterialService materialService, IVariantService variantService,
            IStoreInventoryService storeInventoryService)
        {
            _storeInventoryService = storeInventoryService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _userService = userService;
            _materialService = materialService;
            _variantService = variantService;
        }
        public async Task<StoreInventory> GetItemInStoreAsync(AddItemModel itemModel)
        {
            var item = await _storeInventoryService.Get(x =>
                x.StoreId == itemModel.StoreId && x.MaterialId.ToString().Equals(itemModel.MaterialId) &&
                x.VariantId.ToString().Equals(itemModel.VariantId)).FirstOrDefaultAsync();
            return item;
        }
    }
}
 