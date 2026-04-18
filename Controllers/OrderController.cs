using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;
using Microsoft.EntityFrameworkCore;
using eays.Data;   // your DbContext namespace

namespace eays.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            using (MemoryStream ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                document.SetMargins(36, 36, 36, 36);

                // Define fonts
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Define Colors matching user image
                var primaryColor = new DeviceRgb(29, 78, 216);    // Dark blue for brand
                var textDark = new DeviceRgb(15, 23, 42);         // Dark text
                var textMuted = new DeviceRgb(100, 116, 139);     // Muted text
                var borderColor = new DeviceRgb(226, 232, 240);   // Border
                var lightBg = new DeviceRgb(248, 250, 252);       // Light gray box bg

                // Header Table (Brand & Details)
                Table headerTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth().SetMarginBottom(20);
                
                // Left Brand Cell
                Cell brandCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                Paragraph taxInvoiceLabel = new Paragraph("TAX INVOICE").SetFontSize(10).SetFontColor(textMuted).SetMarginBottom(0);
                brandCell.Add(taxInvoiceLabel);
                
                // Attempt to load the logo image
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    try
                    {
                        var imgData = iText.IO.Image.ImageDataFactory.Create(logoPath);
                        var logoImg = new Image(imgData).SetHeight(35).SetMarginTop(2).SetMarginBottom(2);
                        brandCell.Add(logoImg);
                    }
                    catch
                    {
                        // Fallback if image fails to load
                        Paragraph brandLabel = new Paragraph("EasyCart").SetFontSize(24).SetFontColor(textDark).SetFont(boldFont).SetMarginTop(0).SetMarginBottom(0);
                        brandCell.Add(brandLabel);
                    }
                }
                else
                {
                    // Fallback to text if logo doesn't exist
                    Paragraph brandLabel = new Paragraph("EasyCart").SetFontSize(24).SetFontColor(textDark).SetFont(boldFont).SetMarginTop(0).SetMarginBottom(0);
                    brandCell.Add(brandLabel);
                }

                Paragraph tagline = new Paragraph("Smart Commerce, Fast Delivery").SetFontSize(10).SetFontColor(primaryColor);
                brandCell.Add(tagline);
                
                // Right Invoice Details Cell
                Cell invoiceDetailsCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
                invoiceDetailsCell.Add(new Paragraph($"Invoice No: INV-{order.Id.ToString("X8").ToUpper()}").SetFontSize(11).SetFontColor(textDark).SetFont(boldFont).SetMarginBottom(0));
                invoiceDetailsCell.Add(new Paragraph($"Order ID: {order.Id}").SetFontSize(10).SetFontColor(textDark));
                invoiceDetailsCell.Add(new Paragraph($"Date: {order.OrderDate:dd-MM-yyyy}").SetFontSize(10).SetFontColor(textDark));
                invoiceDetailsCell.Add(new Paragraph($"Status: {order.Status}").SetFontSize(10).SetFontColor(textDark));
                invoiceDetailsCell.Add(new Paragraph($"Payment: {order.PaymentStatus}").SetFontSize(10).SetFontColor(textDark));

                headerTable.AddCell(brandCell);
                headerTable.AddCell(invoiceDetailsCell);
                document.Add(headerTable);

                // Billing Cards (Billed By & Billed To)
                Table billingInfoTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth().SetMarginBottom(20);
                
                // Billed By Box
                Cell billedByCell = new Cell().SetPadding(15).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                billedByCell.Add(new Paragraph("BILLED BY").SetFontSize(10).SetFontColor(textMuted).SetFont(boldFont).SetMarginBottom(5));
                billedByCell.Add(new Paragraph("EasyCart").SetFontSize(11).SetFontColor(textDark).SetFont(boldFont));
                billedByCell.Add(new Paragraph("123 Business Ave, Tech Park 12345").SetFontSize(10).SetFontColor(textDark));
                billedByCell.Add(new Paragraph("Support: support@easycart.com | +91-7600033911").SetFontSize(10).SetFontColor(textDark));
                billedByCell.Add(new Paragraph("GSTIN: 24ABCDE1234F1Z9").SetFontSize(10).SetFontColor(textDark).SetFont(boldFont));
                billedByCell.Add(new Paragraph("State Code: 24").SetFontSize(10).SetFontColor(textDark).SetFont(boldFont));

                // Billed To Box
                Cell billedToCell = new Cell().SetPadding(15).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1)).SetMarginLeft(10);
                billedToCell.Add(new Paragraph("BILLED TO").SetFontSize(10).SetFontColor(textMuted).SetFont(boldFont).SetMarginBottom(5));
                billedToCell.Add(new Paragraph(order.FullName).SetFontSize(11).SetFontColor(textDark).SetFont(boldFont));
                billedToCell.Add(new Paragraph(order.Email).SetFontSize(10).SetFontColor(textDark));
                // Optional handling if order does not have phone
                billedToCell.Add(new Paragraph("Not Provided").SetFontSize(10).SetFontColor(textDark));
                billedToCell.Add(new Paragraph(order.Address).SetFontSize(10).SetFontColor(textDark));

                billingInfoTable.AddCell(billedByCell);
                billingInfoTable.AddCell(billedToCell);
                document.Add(billingInfoTable);

                // Order Items Table
                Table itemTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 6, 2, 2, 2 })).UseAllAvailableWidth().SetMarginBottom(20);
                
                // Table Header
                Cell th1 = new Cell().Add(new Paragraph("#").SetFontSize(10).SetFont(boldFont)).SetPadding(8).SetBackgroundColor(lightBg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                Cell th2 = new Cell().Add(new Paragraph("ITEM").SetFontSize(10).SetFont(boldFont)).SetPadding(8).SetBackgroundColor(lightBg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                Cell th3 = new Cell().Add(new Paragraph("QTY").SetFontSize(10).SetFont(boldFont)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(lightBg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                Cell th4 = new Cell().Add(new Paragraph("UNIT PRICE").SetFontSize(10).SetFont(boldFont)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(lightBg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                Cell th5 = new Cell().Add(new Paragraph("TOTAL").SetFontSize(10).SetFont(boldFont)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT).SetBackgroundColor(lightBg).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                
                itemTable.AddHeaderCell(th1);
                itemTable.AddHeaderCell(th2);
                itemTable.AddHeaderCell(th3);
                itemTable.AddHeaderCell(th4);
                itemTable.AddHeaderCell(th5);

                // Table Rows
                decimal subtotal = 0;
                if (order.OrderItems != null)
                {
                    int index = 1;
                    foreach (var item in order.OrderItems)
                    {
                        var productName = item.Product?.Name ?? "Product";
                        var lineTotal = item.Price * item.Quantity;
                        subtotal += lineTotal;

                        Cell td1 = new Cell().Add(new Paragraph(index.ToString()).SetFontSize(10)).SetPadding(8).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                        Cell td2 = new Cell().Add(new Paragraph(productName).SetFontSize(10)).SetPadding(8).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                        Cell td3 = new Cell().Add(new Paragraph(item.Quantity.ToString()).SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.CENTER).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                        Cell td4 = new Cell().Add(new Paragraph($"Rs. {item.Price:N2}").SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));
                        Cell td5 = new Cell().Add(new Paragraph($"Rs. {lineTotal:N2}").SetFontSize(10)).SetPadding(8).SetTextAlignment(TextAlignment.RIGHT).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1));

                        itemTable.AddCell(td1);
                        itemTable.AddCell(td2);
                        itemTable.AddCell(td3);
                        itemTable.AddCell(td4);
                        itemTable.AddCell(td5);
                        index++;
                    }
                }

                document.Add(itemTable);

                // Summary Block (GST and Totals)
                Table summaryContainer = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth().SetMarginBottom(30);

                // GST Summary (Left)
                Cell gstBox = new Cell().SetPadding(15).SetBorder(new iText.Layout.Borders.SolidBorder(borderColor, 1)).SetBackgroundColor(lightBg);
                gstBox.Add(new Paragraph("GST SUMMARY").SetFontSize(10).SetFontColor(textMuted).SetFont(boldFont).SetMarginBottom(10));

                decimal taxRate = 0.08m; // 8% total
                decimal taxValue = subtotal * taxRate;
                decimal cgstValue = taxValue / 2;
                decimal sgstValue = taxValue / 2;

                // Create inner table for GST breakdown
                Table gstTable = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth();
                
                gstTable.AddCell(new Cell().Add(new Paragraph("Taxable Value").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(2));
                gstTable.AddCell(new Cell().Add(new Paragraph($"Rs. {subtotal:N2}").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2));
                
                gstTable.AddCell(new Cell().Add(new Paragraph("GST Rate").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(2));
                gstTable.AddCell(new Cell().Add(new Paragraph("8%").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2));
                
                gstTable.AddCell(new Cell().Add(new Paragraph("CGST (4%)").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(2));
                gstTable.AddCell(new Cell().Add(new Paragraph($"Rs. {cgstValue:N2}").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2));
                
                gstTable.AddCell(new Cell().Add(new Paragraph("SGST (4%)").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(2));
                gstTable.AddCell(new Cell().Add(new Paragraph($"Rs. {sgstValue:N2}").SetFontSize(10)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(2));

                gstBox.Add(gstTable);
                
                // Final Grand Total (Right)
                Table innerTotals = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1 })).UseAllAvailableWidth();
                
                innerTotals.AddCell(new Cell().Add(new Paragraph("Subtotal").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(4));
                innerTotals.AddCell(new Cell().Add(new Paragraph($"Rs. {subtotal:N2}").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(4));

                innerTotals.AddCell(new Cell().Add(new Paragraph("Tax").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(4));
                innerTotals.AddCell(new Cell().Add(new Paragraph($"Rs. {taxValue:N2}").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(4));

                innerTotals.AddCell(new Cell().Add(new Paragraph("Shipping").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(4));
                innerTotals.AddCell(new Cell().Add(new Paragraph("Free").SetFontSize(11)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPadding(4));

                innerTotals.AddCell(new Cell().Add(new Paragraph("Grand Total").SetFontSize(14).SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPaddingTop(10));
                innerTotals.AddCell(new Cell().Add(new Paragraph($"Rs. {order.TotalAmount:N2}").SetFontSize(14).SetFont(boldFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT).SetPaddingTop(10));

                Cell totalBox = new Cell().Add(innerTotals).SetPadding(15).SetBorder(new iText.Layout.Borders.SolidBorder(textDark, 1.5f)).SetMarginLeft(10);

                summaryContainer.AddCell(gstBox);
                summaryContainer.AddCell(totalBox);
                document.Add(summaryContainer);

                document.Add(new Paragraph("-------------------------------------------------------------------------------------------------------").SetFontColor(borderColor));

                // Footer Notes
                document.Add(new Paragraph("Notes: This is a computer-generated invoice and does not require a physical signature.").SetFontSize(9).SetFontColor(textMuted).SetMarginTop(10));
                document.Add(new Paragraph("Goods once sold will only be returned/replaced as per EasyCart return policy.").SetFontSize(9).SetFontColor(textMuted));
                document.Add(new Paragraph("For support, contact Support: support@easycart.com | +91-7600033911.").SetFontSize(9).SetFontColor(textMuted));

                document.Close();

                return File(ms.ToArray(), "application/pdf", $"Invoice_INV_{order.Id.ToString("X8").ToUpper()}.pdf");
            }
        }
    }
}