using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;

namespace CMMS.API.Helpers
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper()
        {
            #region User
            CreateMap<ApplicationUser, UserDTO>().ReverseMap();
            CreateMap<ApplicationUser, UserRolesVM>().ReverseMap();
            CreateMap<ApplicationUser, UserVM>().ReverseMap();
            CreateMap<ApplicationUser, UserCM>().ReverseMap();
            #endregion


            #region Store 
            CreateMap<StoreDTO, Store>().ReverseMap();
            CreateMap<StoreCM, Store>().ReverseMap();
            CreateMap<StoreVM, Store>().ReverseMap();
            #endregion

            CreateMap<Cart, CartDTO>().ReverseMap();
            CreateMap<Cart, CartVM>().ReverseMap();
            CreateMap<CartDTO, CartVM>().ReverseMap();

		}
    }
}
