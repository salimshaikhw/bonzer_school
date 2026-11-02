using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common;
using Application.Exceptions;
using Domain.Entities.Fees;

namespace Application.Fees
{
    /// <summary>Invoice statuses.</summary>
    public static class InvoiceStatuses
    {
        public const string Open = "Open";
        public const string PartiallyPaid = "PartiallyPaid";
        public const string Paid = "Paid";
    }

    /// <summary>
    /// Keeps invoice balances in step with payments: a payment line that names an invoice item reduces that
    /// item's balance (never below zero, so an invoice cannot be paid twice), and the invoice's paid / remaining
    /// amounts and status are recalculated from its items. Deleting the payment reverses it.
    /// </summary>
    public static class InvoiceSettlement
    {
        private const decimal Tolerance = 0.01m;

        public static void Apply(InvoiceItem item, decimal amount)
        {
            if (amount > item.RemainingAmount + Tolerance)
                throw new BaseServiceException(
                    $"The payment of {amount} is more than the {item.RemainingAmount} still due on this invoice item.", ExceptionCodes.Operation);
            item.PaidAmount += amount;
            item.RemainingAmount = Math.Max(0, item.TotalAmount - item.PaidAmount);
        }

        public static void Reverse(InvoiceItem item, decimal amount)
        {
            item.PaidAmount = Math.Max(0, item.PaidAmount - amount);
            item.RemainingAmount = item.TotalAmount - item.PaidAmount;
        }

        public static void Recalculate(Invoice invoice, IEnumerable<InvoiceItem> items)
        {
            var list = items.ToList();
            invoice.TotalAmount = list.Sum(i => i.TotalAmount);
            invoice.PaidAmount = list.Sum(i => i.PaidAmount);
            invoice.RemainingAmount = list.Sum(i => i.RemainingAmount);
            invoice.Status = invoice.RemainingAmount <= Tolerance ? InvoiceStatuses.Paid
                : invoice.PaidAmount > 0 ? InvoiceStatuses.PartiallyPaid
                : InvoiceStatuses.Open;
        }
    }
}
