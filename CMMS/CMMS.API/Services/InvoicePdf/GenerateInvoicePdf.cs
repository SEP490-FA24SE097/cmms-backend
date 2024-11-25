using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Google.Apis.Storage.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.InvoicePdf
{
    public interface IGenerateInvoicePdf
    {
        Task<string> GenerateHtmlFromInvoiceAsync(string invoiceId);
        byte[] GeneratePdf(string htmlContent);
    }

    public class GenerateInvoicePdf : IGenerateInvoicePdf
    {
        private IInvoiceService _invoiceService;
        private IMaterialVariantAttributeService _materialVariantAttributeService;
        private IVariantService _variantService;
        private IMaterialService _materialService;
        private ITransactionService _transactionService;
        private IInvoiceDetailService _invoiceDetailService;
        private IUserService _userService;
        private IStoreService _storeService;
        private IStoreInventoryService _storeInventoryService;
        private IConverter _converter;
        private IMapper _mapper;
        private IShippingDetailService _shippingDetailService;

        public GenerateInvoicePdf(IInvoiceService invoiceService,
            IShippingDetailService shippingDetailService,
                        IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            ITransactionService transactionService,
            IInvoiceDetailService invoiceDetailService,
            IMapper mapper, IUserService userService,
            IStoreService storeService,
            IStoreInventoryService storeInventoryService,
            IConverter converter
            )
        {
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _invoiceService = invoiceService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _transactionService = transactionService;
            _invoiceDetailService = invoiceDetailService;
            _userService = userService;
            _storeService = storeService;
            _storeInventoryService = storeInventoryService;
            _converter = converter;
        }
        public async Task<string> GenerateHtmlFromInvoiceAsync(string invoiceId)
        {
            var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoiceId),
                _ => _.Invoice, _ => _.Invoice.InvoiceDetails, _ => _.Shipper, _ => _.Shipper.Store).FirstOrDefault();
            if (shippingDetail == null)
            {
                return null;
            }
            var invoiceData = _mapper.Map<ShippingDetailVM>(shippingDetail);
            var invoice = _invoiceService.Get(_ => _.Id.Equals(shippingDetail.Invoice.Id), _ => _.Customer).FirstOrDefault();
            var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
            var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
            invoiceData.Invoice.UserVM = _mapper.Map<UserVM>(invoice.Customer);
            invoiceData.Invoice.StaffId = staff != null ? staff.Id : null;
            invoiceData.Invoice.StaffName = staff != null ? staff.FullName : null;
            invoiceData.Invoice.NeedToPay = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id)).FirstOrDefault().NeedToPay;
            invoiceData.Invoice.StoreName = store.Name;
            invoiceData.Invoice.StoreId = store.Id;
            foreach (var invoiceDetails in invoiceData.Invoice.InvoiceDetails)
            {
                invoiceDetails.StoreId = invoiceData.Invoice.StoreId;
                var addItemModel = _mapper.Map<AddItemModel>(invoiceDetails);
                var storeItem = await _storeInventoryService.GetItemInStoreAsync(addItemModel);
                if (storeItem != null)
                {
                    var material = await _materialService.FindAsync(storeItem.MaterialId);
                    invoiceDetails.ItemName = material.Name;
                    invoiceDetails.SalePrice = material.SalePrice;
                    invoiceDetails.ImageUrl = material.ImageUrl;
                    invoiceDetails.ItemTotalPrice = invoiceDetails.ItemTotalPrice;
                    if (storeItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(storeItem.VariantId)).FirstOrDefault();
                        var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                        invoiceDetails.ItemName += $" | {variantAttribute.Value}";
                        invoiceDetails.SalePrice = variant.Price;
                        invoiceDetails.ImageUrl = variant.VariantImageUrl;
                        invoiceDetails.ItemTotalPrice = invoiceDetails.ItemTotalPrice;
                    }
                }
            }

            var html = $@"
    <!DOCTYPE html>
    <html lang='en'>
    <head>
        <title>Hóa Đơn</title>
        <style>
            body {{ font-family: Arial, sans-serif; margin: 40px; }}
            .header {{ text-align: right; font-weight: bold; }}
            .header h1 {{ text-align: center; font-size: 24px; }}
            .company-info, .shipping-info {{ margin-top: 20px; }}
            .company-info p, .shipping-info p {{ margin: 5px 0; }}
            .special-instructions {{ margin-top: 20px; font-style: italic; }}
            .table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
            .table, .table th, .table td {{ border: 1px solid black; }}
            .table th, .table td {{ padding: 8px; text-align: left; }}
            .footer {{ margin-top: 20px; }}
            .footer p {{ margin: 5px 0; }}
            .thank-you {{ text-align: center; font-weight: bold; margin-top: 20px; }}
        </style>
    </head>
    <body>
        <div class='header'>
            <h1>HÓA ĐƠN</h1>
            <p>HÓA ĐƠN SỐ {invoiceData.Invoice.Id}</p>
            <p>NGÀY: {invoiceData.Invoice.InvoiceDate.ToString("dd/MM/yyyy")}</p>
        </div>
        <div class='company-info'>
            <p>{invoiceData.Invoice.StoreName}</p>
            <p>{invoiceData.Invoice.StoreId}</p>
        </div>
    </body>
            </html>";
            return html;
        }

        public byte[] GeneratePdf(string htmlContent)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 18, Bottom = 18 },
            };

            var objectSettings = new ObjectSettings
            {
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontSize = 10, Right = "Page [page] of [toPage]", Line = true },
                FooterSettings = { FontSize = 8, Center = "PDF demo", Line = true },
            };

            var htmlToPdfDocument = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings },
            };

            return _converter.Convert(htmlToPdfDocument);
        }

   
    }
}
    


