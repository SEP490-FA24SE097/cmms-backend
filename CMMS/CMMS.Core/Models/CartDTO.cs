
namespace CMMS.Core.Models
{
    public class CartDTO
    {
        public string Id { get; set; }
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string CustomerId { get; set; }
        public decimal Quantity { get; set; }
        public double TotalAmount { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }

    public class CartVM
    {
        public string Id { get; set; }
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string CustomerId { get; set; }
        public decimal Quantity { get; set; }
        public string ImageUrl { get; set; }
        public double TotalAmount { get; set; }
    }

    public class AddItemModel
    {
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string? StoreId { get; set; }
    }

    public class CartItemModel : AddItemModel
    {
        public decimal Quantity { get; set; }
    }

    public class CartItemVM : CartItemModel
    {
        public string ItemName { get; set; }
        public string ImageUrl { get; set; }
        public decimal? InOrder { get; set; }
        public decimal SalePrice { get; set; }
        public decimal ItemTotalPrice { get; set; }
        public bool IsChangeQuantity { get; set; } = false;
        public decimal? InStock { get; set; }
    }


    public class CustomerUpdateItemInCartModel
    {
        public string CustomerId { get; set; }
        public string MaterialId { get; set; }
        public string? VariantId { get; set; }
        public string Quantity { get; set; }
    }

    public class PreCheckOutModel 
    {
        public decimal? TotalAmount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal? ShippingFee { get; set; }
        public List<PreCheckOutItemCartModel> Items { get; set; }
    }

    public class PreCheckOutItemCartModel
    {
        public string? StoreId { get; set; }
        public string? StoreName { get; set; }
        public decimal? TotalStoreAmount { get; set; }
        public decimal? ShippngFree { get; set; }
        public decimal? FinalPrice { get; set; }
        public decimal? ShippingDistance { get; set; }
        public List<CartItemVM> StoreItems { get; set; }    
    }

}
