using System;

namespace AutoCare.Domain.Exceptions
{
    public class DomainValidationException : Exception
    {
        public string PropertyName { get; }

        public DomainValidationException(string message) : base(message)
        {
        }

        public DomainValidationException(string propertyName, string message) : base(message)
        {
            PropertyName = propertyName;
        }

        public DomainValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class OptimisticConcurrencyException : Exception
    {
        public string EntityName { get; }
        public object EntityKey { get; }

        public OptimisticConcurrencyException(string entityName, object entityKey)
            : base($"حدث تعارض في التزامن (Optimistic Concurrency Conflict) في الكيان '{entityName}' ذي المفتاح '{entityKey}'. تم تعديل السجل بواسطة عملية أخرى بالتزامن.")
        {
            EntityName = entityName;
            EntityKey = entityKey;
        }

        public OptimisticConcurrencyException(string message) : base(message)
        {
        }

        public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
