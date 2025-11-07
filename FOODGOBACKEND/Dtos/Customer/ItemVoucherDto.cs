namespace FOODGOBACKEND.Dtos.Customer
{
    /// <summary>
    /// Represents a voucher item displayed to customers.
    /// </summary>
    public class ItemVoucherDto
    {
        /// <summary>
        /// The unique identifier for the voucher.
        /// </summary>
        public int VoucherId { get; set; }

        /// <summary>
        /// The voucher code that customers can use.
        /// </summary>
        public string VoucherCode { get; set; } = null!;

        /// <summary>
        /// Description of the voucher (e.g., discount details and conditions).
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The expiration date of the voucher (formatted as string, e.g., "HSD: 30/11/2025").
        /// </summary>
        public string ValidTo { get; set; } = null!;
    }
}