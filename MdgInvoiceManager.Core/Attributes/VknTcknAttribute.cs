using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MdgInvoiceManager.Core.Attributes;

public class VknTcknAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        string vknTckn = value.ToString()!.Trim();

        // Sadece rakamlardan oluşmalı ve tam olarak 10 hane (VKN) veya 11 hane (TCKN) olmalı
        if (!Regex.IsMatch(vknTckn, @"^\d{10}$|^\d{11}$"))
        {
            return new ValidationResult("VKN / TCKN alanı sadece rakamlardan oluşmalı, 10 haneli (VKN) veya 11 haneli (TCKN) olmalıdır.");
        }

        return ValidationResult.Success;
    }
}