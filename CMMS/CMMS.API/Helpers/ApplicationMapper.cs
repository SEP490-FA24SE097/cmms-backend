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
            CreateMap<ApplicationUser, UserStoreVM>()
                 .ForMember(dest => dest.StoreCreateName, opt => opt.MapFrom(src => src.Store.Name))
                .ReverseMap();

            CreateMap<ApplicationUser, ShipperVM>().ReverseMap();
            #endregion


            #region Store 
            CreateMap<StoreDTO, Store>().ReverseMap();
            CreateMap<StoreCM, Store>().ReverseMap();
            CreateMap<StoreVM, Store>().ReverseMap();
            #endregion
            CreateMap<CartItemModel, AddItemModel>().ReverseMap();
            CreateMap<CartDTO, CartVM>().ReverseMap();
            CreateMap<CartItemVM, CartItem>().ReverseMap();
            CreateMap<CartItemWithoutStoreId, CartItemVM>().ReverseMap();
            CreateMap<CartItemWithoutStoreId, AddItemModel>().ReverseMap();
            CreateMap<CartItemWithoutStoreId, CartItem>().ReverseMap();
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
                .ForMember(dest => dest.ShipperName, opt => opt.MapFrom(src => src.Shipper.FullName))
                .ForMember(dest => dest.ShipperCode, opt => opt.MapFrom(src => src.Shipper.Id))
                .ReverseMap();
            CreateMap<ShippingDetail, ShippingDetaiInvoicelVM>()
                .ForMember(dest => dest.ShipperName, opt => opt.MapFrom(src => src.Shipper.FullName))
                .ForMember(dest => dest.ShipperCode, opt => opt.MapFrom(src => src.Shipper.Id))
                .ReverseMap();

            #endregion
            #region Invoice
            CreateMap<Invoice, InvoiceVM>()
                .ForMember(dest => dest.UserVM, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.InvoiceDetails, opt => opt.MapFrom(src => src.InvoiceDetails))
                .ReverseMap();


            CreateMap<Invoice, InvoiceTransactionVM>()
            .ReverseMap();

            CreateMap<Invoice, InvoiceShippingDetailsVM>()
            .ForMember(dest => dest.UserVM, opt => opt.MapFrom(src => src.Customer))
            .ForMember(dest => dest.InvoiceDetails, opt => opt.MapFrom(src => src.InvoiceDetails))
            .ReverseMap();

            CreateMap<InvoiceDetail, InvoiceDetailVM>()
            .ForMember(dest => dest.ItemTotalPrice, opt => opt.MapFrom(src => src.LineTotal))
                .ReverseMap();
            CreateMap<InvoiceDetail, InvoiceShippingDetailsVM>().ReverseMap();
            CreateMap<AddItemModel, InvoiceDetailVM>().ReverseMap();
            CreateMap<InvoiceDetail, CartItem>().ReverseMap();
            #endregion

            #region Transaction
            CreateMap<Transaction, TransactionVM>().ReverseMap();
            #endregion


        }
    }
}
