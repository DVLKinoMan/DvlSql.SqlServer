﻿using System;

namespace DvlSql.SqlServer.Classes
{
    public class SomeClass : IEquatable<SomeClass>
    {
        public int SomeIntField { get; set; }
        public string? SomeStringField { get; set; }

        public SomeClass() { }

        public SomeClass(int f1, string f2) =>
            (SomeIntField, SomeStringField) = (f1, f2);

        public bool Equals(SomeClass? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return SomeIntField == other.SomeIntField && SomeStringField == other.SomeStringField;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SomeClass) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SomeIntField, SomeStringField);
        }
    }
}