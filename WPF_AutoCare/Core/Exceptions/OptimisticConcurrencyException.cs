using System;

namespace C__project_Auto_Care.Core.Exceptions
{
    /// <summary>
    /// يتم إطلاق هذا الاستثناء عند حدوث تعارض في التزامن المتفائل (Optimistic Concurrency Conflict)
    /// نتيجة تعديل نفس السجل من قبل مستخدم أو جلسة أخرى، اعتماداً على الـ RowVersion.
    /// </summary>
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
