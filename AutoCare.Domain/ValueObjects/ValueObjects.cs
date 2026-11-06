using System;
using System.Collections.Generic;
using System.Linq;
using AutoCare.Domain.Common;
using AutoCare.Domain.Exceptions;

namespace AutoCare.Domain.ValueObjects
{
    /// <summary>
    /// كائن قيمة رقم لوحة المركبة - يفرض قاعدة الـ 7 أرقام عشرية حصراً ومطابقة تامة.
    /// </summary>
    public sealed class PlateNumber : ValueObject
    {
        public string Value { get; }

        private PlateNumber(string value)
        {
            Value = value;
        }

        public static PlateNumber Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new DomainValidationException("PlateNumber", "رقم اللوحة لا يمكن أن يكون فارغاً.");

            string trimmed = input.Trim();
            if (trimmed.Length != 7 || !trimmed.All(char.IsDigit))
                throw new DomainValidationException("PlateNumber", $"رقم اللوحة يجب أن يتكون من 7 أرقام عشرية حصراً. المدخل: '{input}'");

            return new PlateNumber(trimmed);
        }

        public static bool TryCreate(string input, out PlateNumber plateNumber, out string error)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                plateNumber = null;
                error = "رقم اللوحة لا يمكن أن يكون فارغاً.";
                return false;
            }

            string trimmed = input.Trim();
            if (trimmed.Length != 7 || !trimmed.All(char.IsDigit))
            {
                plateNumber = null;
                error = "رقم اللوحة يجب أن يتكون من 7 أرقام عشرية حصراً.";
                return false;
            }

            plateNumber = new PlateNumber(trimmed);
            error = null;
            return true;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(PlateNumber plateNumber) => plateNumber?.Value;
    }

    /// <summary>
    /// كائن قيمة رقم الهيكل (VIN) - يفرض قاعدة الـ 17 رمزاً بالضبط.
    /// </summary>
    public sealed class VinNumber : ValueObject
    {
        public string Value { get; }

        private VinNumber(string value)
        {
            Value = value;
        }

        public static VinNumber Create(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new DomainValidationException("VinNumber", "رقم الهيكل (VIN) لا يمكن أن يكون فارغاً.");

            string trimmed = input.Trim().ToUpperInvariant();
            if (trimmed.Length != 17)
                throw new DomainValidationException("VinNumber", $"رقم الهيكل يجب أن يتكون من 17 رمزاً بالضبط. المدخل: '{input}' بطول {trimmed.Length}");

            return new VinNumber(trimmed);
        }

        public static bool TryCreate(string input, out VinNumber vinNumber, out string error)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                vinNumber = null;
                error = "رقم الهيكل (VIN) لا يمكن أن يكون فارغاً.";
                return false;
            }

            string trimmed = input.Trim().ToUpperInvariant();
            if (trimmed.Length != 17)
            {
                vinNumber = null;
                error = "رقم الهيكل يجب أن يتكون من 17 رمزاً بالضبط.";
                return false;
            }

            vinNumber = new VinNumber(trimmed);
            error = null;
            return true;
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(VinNumber vinNumber) => vinNumber?.Value;
    }

    /// <summary>
    /// كائن قيمة المبالغ المالية - يمنع القيم السالبة ويضمن دقة الحسابات العشرية.
    /// </summary>
    public sealed class Money : ValueObject, IComparable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public static readonly Money Zero = new Money(0, "LYD");

        public Money(decimal amount, string currency = "LYD")
        {
            if (amount < 0)
                throw new DomainValidationException("Money", "المبلغ المالي لا يمكن أن يكون سالباً.");

            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
            Currency = string.IsNullOrWhiteSpace(currency) ? "LYD" : currency.Trim();
        }

        public static Money FromDecimal(decimal amount, string currency = "LYD")
        {
            return new Money(amount, currency);
        }

        public Money Add(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            EnsureSameCurrency(other);
            decimal result = Amount - other.Amount;
            if (result < 0)
                result = 0; // Clamped to zero by business invariant
            return new Money(result, Currency);
        }

        public Money Multiply(decimal multiplier)
        {
            if (multiplier < 0)
                throw new DomainValidationException("Money", "معامل الضرب لا يمكن أن يكون سالباً.");

            return new Money(Amount * multiplier, Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            if (!string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase))
                throw new DomainValidationException("Money", $"لا يمكن إجراء عمليات بين عملتين مختلفتين: '{Currency}' و '{other.Currency}'.");
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Amount;
            yield return Currency.ToUpperInvariant();
        }

        public int CompareTo(Money other)
        {
            if (other == null) return 1;
            EnsureSameCurrency(other);
            return Amount.CompareTo(other.Amount);
        }

        public static Money operator +(Money a, Money b) => a.Add(b);
        public static Money operator -(Money a, Money b) => a.Subtract(b);
        public static Money operator *(Money a, decimal b) => a.Multiply(b);
        public static bool operator <(Money a, Money b) => a.CompareTo(b) < 0;
        public static bool operator <=(Money a, Money b) => a.CompareTo(b) <= 0;
        public static bool operator >(Money a, Money b) => a.CompareTo(b) > 0;
        public static bool operator >=(Money a, Money b) => a.CompareTo(b) >= 0;

        public override string ToString() => (Currency == "LYD" || Currency == "د.ل") ? $"{Amount:N2} د.ل" : $"{Amount:N2} {Currency}";
        public static implicit operator decimal(Money money) => money?.Amount ?? 0m;
    }
}
