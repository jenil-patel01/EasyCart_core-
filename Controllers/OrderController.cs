using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;
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

        public IActionResult DownloadInvoice(int orderId)
        {
            var order = _context.Orders
                .FirstOrDefault(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            using (MemoryStream ms = new MemoryStream())
            {
                var writer = new PdfWriter(ms);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Title (bold fix)
                var title = new Paragraph("EasyCart Invoice");
                title.SetFontSize(18);
                document.Add(title);

                document.Add(new Paragraph("--------------------------------"));
                document.Add(new Paragraph($"Order ID: {order.Id}"));
                document.Add(new Paragraph($"Customer: {order.FullName}"));
                document.Add(new Paragraph($"Email: {order.Email}"));
                document.Add(new Paragraph($"Address: {order.Address}"));
                document.Add(new Paragraph($"Date: {order.OrderDate:dd-MM-yyyy}"));
                document.Add(new Paragraph($"Total: ₹{order.TotalAmount}"));
                document.Add(new Paragraph("--------------------------------"));
                document.Add(new Paragraph("Thank you for shopping with EasyCart"));

                document.Close();

                return File(ms.ToArray(), "application/pdf", $"Invoice_{order.Id}.pdf");
            }
        }
    }
}