using System;

namespace AutoCare.Domain.Common
{
    public interface IAggregateRoot
    {
    }

    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }

    public abstract class Entity<TId>
    {
        public TId Id { get; protected set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Entity<TId> other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (Id == null || other.Id == null)
                return false;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return (GetType().ToString() + Id).GetHashCode();
        }

        public static bool operator ==(Entity<TId> left, Entity<TId> right)
        {
            if (Equals(left, null))
                return Equals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId> left, Entity<TId> right)
        {
            return !(left == right);
        }
    }

    public abstract class ValueObject
    {
        protected abstract System.Collections.Generic.IEnumerable<object> GetAtomicValues();

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var other = (ValueObject)obj;
            using (var thisValues = GetAtomicValues().GetEnumerator())
            using (var otherValues = other.GetAtomicValues().GetEnumerator())
            {
                while (thisValues.MoveNext() && otherValues.MoveNext())
                {
                    if (ReferenceEquals(thisValues.Current, null) ^ ReferenceEquals(otherValues.Current, null))
                        return false;

                    if (thisValues.Current != null && !thisValues.Current.Equals(otherValues.Current))
                        return false;
                }

                return !thisValues.MoveNext() && !otherValues.MoveNext();
            }
        }

        public override int GetHashCode()
        {
            int hash = 17;
            foreach (var obj in GetAtomicValues())
            {
                hash = hash * 31 + (obj != null ? obj.GetHashCode() : 0);
            }
            return hash;
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            if (Equals(left, null))
                return Equals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !(left == right);
        }
    }
}
