namespace CMMS.Infrastructure.Enums
{
    public enum Permission
    {
        ViewDashboard,
        ManageUsers,
        ManageRoles,
        ManageMeterials,
        StoreMaterialTrackings,
        ManageStores,
        StoreMaterialTracking,
        OderTracking,
        ManageInventory,
        InventoryTracking,
        ManageProfile,
        CreateOrder,
        ViewOrderHistory,
    }

    public enum SeniorManagementPermission { 
        StoreMaterialTrackings,
   
    }

    public enum  StoreManagerPermission 
    {
        ManageStores,
        StoreMaterialTracking,
    }

    public enum SaleStaffPermission
    {
        OderTracking,

    }
    public enum WarehouseStaffPermission {
        ManageInventory,
        InventoryTracking,
    }

    public enum CustomerPermission { 
        ManageProfile,
        CreateOrder,
        ViewOrderHistory,
    }



}
