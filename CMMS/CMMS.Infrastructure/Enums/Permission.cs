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
        CreateShipper,
        CreateSaleStaff,
        GetSaleStaff,
        SendRequestToImport,
        ViewCustomer
    }

    public enum SeniorManagementPermission { 
        StoreMaterialTrackings,
        SendRequestToImport,
        CreateShipper,
        CreateSaleStaff,
        GetSaleStaff,
        ViewCustomer

    }

    public enum  StoreManagerPermission 
    {
        ManageStores,
        StoreMaterialTracking,
    }

    public enum SaleStaffPermission
    {
        OderTracking,
        ViewCustomer,
        CreateShipper

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
