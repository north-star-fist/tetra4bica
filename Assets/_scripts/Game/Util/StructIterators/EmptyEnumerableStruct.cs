using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Struct alternative for LINQ <see cref="Enumerable.Empty{T}"/>.
/// </summary>
public struct EmptyEnumerableStruct<T> : IEnumerable<T> {

    public EmptyEnumerator<T> GetEnumerator() {
        return new EmptyEnumerator<T>();
    }

#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();
#pragma warning restore HAA0401 // Possible allocation of reference type enumerator
}

public struct EmptyEnumerator<T> : IEnumerator<T> {

    public T Current => throw new InvalidOperationException();

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
    object IEnumerator.Current => this.Current;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

    public bool MoveNext() => false;

    public void Dispose() { }

    public void Reset() { }
}