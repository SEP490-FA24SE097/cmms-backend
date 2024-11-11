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
            CreateMap<CartItemModel, AddItemModel>().ReverseMap();
            CreateMap<CartDTO, CartVM>().ReverseMap();
            CreateMap<CartItemVM, CartItem>().ReverseMap();
            CreateMap<CartItem, AddItemModel>().ReverseMap();

            #region CustomerBalance 
            CreateMap<CustomerBalance, CustomerBalanceVM>()
                .ForMember(dest => dest.UserVM, opt => opt.MapFrom(src => src.Customer))
                .ReverseMap();
            CreateMap<CustomerBalance, CustomerBalanceDTO>().ReverseMap();
            CreateMap<CustomerBalanceVM, CustomerBalanceDTO>().ReverseMap();
            #endregion

            #region ShippingDetail 
            CreateMap<ShippingDetail, ShippingDetailVM>()
                .ForMember(dest => dest.Invoice, opt => opt.MapFrom(src => src.Invoice))
                .ReverseMap();
            #endregion


        }
    }
}
