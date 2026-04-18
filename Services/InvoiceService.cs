using eays.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace eays.Services
{
    public interface IInvoiceService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Order order);
    }

    public class InvoiceService : IInvoiceService
    {
        public Task<byte[]> GenerateInvoicePdfAsync(Order order)
        {
            return Task.Run(() =>
            {
                var memoryStream = new MemoryStream();

                using (PdfWriter writer = new PdfWriter(memoryStream))
                {
                    using (PdfDocument pdf = new PdfDocument(writer))
                    {
                        Document document = new Document(pdf);

                        // Title
                        Paragraph title = new Paragraph("INVOICE")
                            .SetFontSize(24)
                            .SetFont(PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD))
                            .SetTextAlignment(TextAlignment.CENTER);
                        document.Add(title);

                        // Invoice details
                        Table headerTable = new Table(2);
                        var boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
                        headerTable.AddCell(new Cell().Add(new Paragraph("Invoice #: " + order.Id).SetFont(boldFont)));
                        headerTable.AddCell(new Cell().Add(new Paragraph("Date: " + order.OrderDate.ToString("dd/MM/yyyy"))));
                        headerTable.AddCell(new Cell().Add(new Paragraph("Order Status: " + order.Status)));
                        headerTable.AddCell(new Cell().Add(new Paragraph("Payment Status: " + (order.PaymentStatus ?? "Pending"))));
                        document.Add(headerTable);

                        document.Add(new Paragraph("\n"));

                        // Customer details
                        Paragraph customerTitle = new Paragraph("BILLING DETAILS")
                            .SetFont(boldFont)
                            .SetFontSize(12);
                        document.Add(customerTitle);

                        Table customerTable = new Table(1);
                        customerTable.AddCell(new Cell().Add(new Paragraph("Name: " + order.FullName)));
                        customerTable.AddCell(new Cell().Add(new Paragraph("Email: " + order.Email)));
                        customerTable.AddCell(new Cell().Add(new Paragraph("Phone: " + order.PhoneNumber)));
                        customerTable.AddCell(new Cell().Add(new Paragraph("Address: " + order.Address)));
                        document.Add(customerTable);

                        document.Add(new Paragraph("\n"));

                        // Order items table
                        Paragraph itemsTitle = new Paragraph("ORDER ITEMS")
                            .SetFont(boldFont)
                            .SetFontSize(12);
                        document.Add(itemsTitle);

                        Table itemsTable = new Table(4);
                        itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Product").SetFont(boldFont)));
                        itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantity").SetFont(boldFont)));
                        itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Price").SetFont(boldFont)));
                        itemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Total").SetFont(boldFont)));

                        if (order.OrderItems != null)
                        {
                            foreach (var item in order.OrderItems)
                            {
                                itemsTable.AddCell(new Cell().Add(new Paragraph(item.Product?.Name ?? "N/A")));
                                itemsTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())));
                                itemsTable.AddCell(new Cell().Add(new Paragraph("?" + item.Price.ToString("F2"))));
                                itemsTable.AddCell(new Cell().Add(new Paragraph("?" + (item.Price * item.Quantity).ToString("F2"))));
                            }
                        }

                        document.Add(itemsTable);

                        document.Add(new Paragraph("\n"));

                        // Total
                        Paragraph total = new Paragraph("Total Amount: ?" + order.TotalAmount.ToString("F2"))
                            .SetFontSize(14)
                            .SetFont(boldFont)
                            .SetTextAlignment(TextAlignment.RIGHT);
                        document.Add(total);

                        document.Close();
                    }
                }

                return memoryStream.ToArray();
            });
        }
    }
}
