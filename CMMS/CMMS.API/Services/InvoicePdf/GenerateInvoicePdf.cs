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
        //Task<byte[]> GeneratePdf(string htmlContent);
       byte[] GeneratePdf(string htmlContent, string fileName);
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
            <p><b>HÓA ĐƠN SỐ:</b> {invoiceData.Invoice.Id}</p>
            <p><b>NGÀY:</b>{invoiceData.Invoice.InvoiceDate.ToString("dd/MM/yyyy")}</p>       
            <p><b>ĐỊA CHỈ MUA:</b> {invoiceData.Invoice.BuyIn}</p>

        </div>

        <div class='shipping-info'>
            <div style='float: left; width: 45%;'>
                <p><b>ĐẾN:</b> {invoiceData.Invoice.UserVM.FullName}</p>
                <p><b>ĐỊA CHỈ:</b> {invoiceData.Address}</p>        
                <p><b>DỰ KIẾN GIAO:</b> {invoiceData.EstimatedArrival.ToString("dd/MM/yyyy")}</p>        
                <p><b>GIAO HÀNG VÀO:</b> {(invoiceData.ShippingDate != null ? invoiceData.EstimatedArrival.ToString("dd/MM/yyyy") : "")}</p>
                <p><b>SỐ ĐIỆN THOẠI NHẬN HÀNG:</b> {invoiceData.PhoneReceive}</p>
            </div>
            <div style='float: right; width: 45%;'>
                <p><b>CỬA HÀNG:</b> {store.Name}</p>
                <p><b>ĐỊA CHỈ:</b> {store.Address}</p>        
            </div>
            <div style='clear: both;'></div>
        </div>
        <div class='special-instructions'>
            <p>CHÚ THÍCH HOẶC HƯỚNG DẪN ĐẶC BIỆT:</p>
            <p>{invoiceData.Note}</p>
        </div>
        <table class='table'>
            <tr>
                <th>NGƯỜI BÁN</th>
                <th>SỐ ĐƠN HÀNG</th>
                <th>NGƯỜI YÊU CẦU</th>
                <th>ĐÃ VẬN</th>
                <th>ĐIỂM F.O.B</th>
                <th>ĐIỀU</th>
            </tr>
            <tr>
                <td>{(invoiceData.Invoice.StaffName != null ? invoiceData.Invoice.StaffName : invoiceData.Invoice.BuyIn)}</td>
                <td>{invoiceData.Invoice.Id}</td>
                <td>{invoiceData.Invoice.UserVM.FullName}</td>
                <td>X</td>
                <td>-</td>
                <td>[Thanh toán khi nhận]</td>
            </tr>
        </table>
        <table class='table'>
            <tr>
                <th>SỐ LƯỢNG</th>
                <th>MÔ TẢ</th>
                <th>ĐƠN GIÁ</th>
                <th>TỔNG</th>
            </tr>";

            // Thêm các sản phẩm vào bảng
            foreach (var item in invoiceData.Invoice.InvoiceDetails)
            {
                html += $@"
            <tr>
                <td>{item.Quantity}</td>
                <td>{item.ItemName}</td>
                <td>{item.SalePrice:C}</td>
                <td>{item.ItemTotalPrice:C}</td>
            </tr>";
            }

            html += $@"
            </table>
            <div class='footer'>
                <p><b>TỔNG THU:</b> {invoiceData.Invoice.TotalAmount:C}</p>
                <p><b>GIẢM GIÁ:</b> {invoiceData.Invoice.Discount:C}</p>
                <p><b>VẬN CHUYỂN & ĐÓNG GÓI TRONG VẬN CHUYỂN:</b> 0</p>
                <p><b>KHÁCH TRẢ TRƯỚC:</b> {invoiceData.Invoice.CustomerPaid:C}</p>         
                <p><b>TỔNG SỐ TIỀN PHẢI THANH TOÁN:</b> {invoice.SalePrice:C}</p>

                <p>Nếu bạn có bất kỳ câu hỏi nào về hóa đơn này, hãy liên hệ [Tên, số điện thoại, email]</p>
            </div>
            <div class='thank-you'>
                <p><b>CẢM ƠN BẠN ĐÃ GIAO DỊCH VỚI CHÚNG TÔI!</b></p>
            </div>
        </body>
    </html>";

            return html;
        }

        //public async Task<byte[]> GeneratePdf(string htmlContent)
        public byte[] GeneratePdf(string htmlContent, string fileName)
        {
            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 18, Bottom = 18 },
                Out = Path.Combine(Directory.GetCurrentDirectory(), "Exports","Invoices", fileName)
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
            //var content = new StringContent(htmlContent, Encoding.UTF8, "application/json");
            //var response = await _httpClient.PostAsync("/api/convert", content);

            //response.EnsureSuccessStatusCode();
            //return await response.Content.ReadAsByteArrayAsync();
        }


    }
}



