namespace CMMS.Infrastructure.Enums
{
    public enum PermissionName
    {
        SeniorPermission,
        ViewDashboard,
        ViewDashboardInStore,
        ViewCustomer,
        AddNewCustomer,
        UpdateCustomerStatus,
        ViewMaterial,
        EditMaterial,
        UpdateMaterialStatus,
        CreateInvoice,
        UpdateInvoice,
        RefundInvoice,
        ViewShippingDetails,
        SendRequestToChangeShipInvoice,
        AddEmployee,

        ManageInventory,
        InventoryTracking,

        CustomerPermissions,
        StoreShipperPermissions,

        GoodNotePermissions,
        ImportPermissions,

        InvoicePermissions,
        ImportRequestPermissions
    }

    public enum SeniorManagementPermission
    {
        SeniorPermission,
        ViewDashboard,
        ViewDashboardInStore,
        ViewCustomer,
        AddNewCustomer,
        UpdateCustomerStatus,
        ViewMaterial,
        EditMaterial,
        UpdateMaterialStatus,
        CreateInvoice,
        UpdateInvoice,
        RefundInvoice,
        ViewShippingDetails,
        SendRequestToChangeShipInvoice,
        AddEmployee,
        ManageInventory,
        InventoryTracking,
        CustomerPermissions,
        StoreShipperPermissions,
        GoodNotePermissions,
        ImportPermissions,
        InvoicePermissions,
        ImportRequestPermissions,
        
    }

    public enum StoreManagerPermission
    {
        ViewCustomer,
        AddNewCustomer,
        CreateInvoice,
        UpdateInvoice,
        RefundInvoice,
        ViewDashboardInStore,
        ManageInventory,
        InventoryTracking,
        GoodNotePermissions,
        ImportPermissions,
        InvoicePermissions,
        CustomerPermissions,
        ImportRequestPermissions
    }

    public enum SaleStaffPermission
    {
        ViewCustomer,
        AddNewCustomer,
        CreateInvoice,
        UpdateInvoice,
        RefundInvoice,
        ManageInventory,
        InventoryTracking,
        InvoicePermissions,
        CustomerPermissions
    }

    public enum CustomerPermission
    {
        CustomerPermissions,
        InvoicePermissions
    }

    public enum StoreShipperPermission
    {
        StoreShipperPermissions,
        InvoicePermissions
    }


}
