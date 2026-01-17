﻿namespace smart_receipt_api.Models
{
    public class ReceiptItems
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;   // product name
        public decimal Price { get; set; }                        // total price for this item
        public decimal Quantity { get; set; } = 1;                // quantity (AD or KG)
        public decimal UnitPrice { get; set; }                    // price per unit
        public string? Barcode { get; set; }                      // barcode (optional)
        public string? Unit { get; set; }                         // unit type: "AD" or "KG"

        // which receipt does this item belong to?
        public int ReceiptId { get; set; }
        public Receipt? Receipt { get; set; }
    }
}
