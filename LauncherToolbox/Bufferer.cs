using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Leayal.PSO2Launcher.Toolbox
{
    /// <summary>A simple implementation for buffering <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The type of the item to buffer.</typeparam>
    /// <remarks>This class is not thread-safe.</remarks>
    public class Bufferer<T> : IDisposable, IBufferWriter<T>
    {
        private readonly ArrayPool<T>? _pool;

        private bool disposed;
        private T[]? arr;
        private int pos;

        /// <summary>Gets a <seealso cref="ReadOnlyMemory{T}"/> of the underlying written data.</summary>
        /// <exception cref="ObjectDisposedException">This property is accessed after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public ReadOnlyMemory<T> WrittenMemory
        {
            get
            {
                this.EnsureNotDisposed();
                if (this.pos == 0 || this.arr == null)
                {
                    return ReadOnlyMemory<T>.Empty;
                }
                else
                {
                    return new ReadOnlyMemory<T>(this.arr, 0, this.pos);
                }
            }
        }

        /// <summary>Gets a <seealso cref="ReadOnlySpan{T}"/> of the underlying written data.</summary>
        public ReadOnlySpan<T> WrittenSpan => this.WrittenMemory.Span;

        /// <summary>Gets the amount of data written to the underlying buffer.</summary>
        /// <exception cref="ObjectDisposedException">This property is accessed after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public int WrittenCount
        {
            get
            {
                this.EnsureNotDisposed();
                return this.pos;
            }
        }

        /// <summary>Gets the number of available space that can be written without force the underlying buffer to grow.</summary>
        /// <exception cref="ObjectDisposedException">This property is accessed after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public int FreeCapacity
        {
            get
            {
                this.EnsureNotDisposed();
                if (this.arr == null)
                {
                    return 0;
                }
                else
                {
                    return (this.arr.Length - this.pos);
                }
            }
        }

        /// <summary>Initialize a new instance of this class without pools.</summary>
        public Bufferer() : this(null) { }

        /// <summary>Initialize a new instance of this class without pools.</summary>
        /// <param name="initialCapacity">The initial capacity of the underlying buffer.</param>
        public Bufferer(int initialCapacity) : this(initialCapacity, null) { }

        /// <summary>Initialize a new instance of this class with a given pool.</summary>
        /// <param name="pool">The pool of <typeparamref name="T"/> to rent and return. If null is given, no pooling.</param>
        public Bufferer(ArrayPool<T>? pool) : this(0, pool) { }

        /// <summary>Initialize a new instance of this class with a given pool.</summary>
        /// <param name="initialCapacity">The initial capacity of the underlying buffer. If <paramref name="initialCapacity"/> is 0, no preallocation happens. Default is 0.</param>
        /// <param name="pool">The pool of <typeparamref name="T"/> to rent and return. If null is given, no pooling.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCapacity"/> is less than 0.</exception>
        public Bufferer(int initialCapacity, ArrayPool<T>? pool)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Cannot be less than zero");
            }

            this.disposed = false;
            this.pos = 0;
            this._pool = pool;

            if (initialCapacity == 0)
            {
                this.arr = null;
            }
            else
            {
                this.arr = pool?.Rent(initialCapacity) ?? new T[initialCapacity];
            }
        }

        /// <summary>Notifies the <seealso cref="Buffer"/> that count data items were written.</summary>
        /// <param name="count">The number of data items written.</param>
        /// <exception cref="InvalidOperationException">Called this method before any writing data is present.</exception>
        /// <exception cref="ArgumentException"><paramref name="count"/> should be more than zero.</exception>
        /// <exception cref="ObjectDisposedException">This method is called after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public void Advance(int count)
        {
            this.EnsureNotDisposed();
            if (this.arr == null)
            {
                throw new InvalidOperationException("Cannot advance before any data is written.");
            }
            else if (count <= 0)
            {
                throw new ArgumentException("The param should be more than zero.", nameof(count));
            }
            else if (count > this.FreeCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Advancing beyond the free capacity.");
            }
            else
            {
                this.pos += count;
            }
        }

        /// <summary>Reset the position back to zero without clearing the underlying data.</summary>
        /// <exception cref="ObjectDisposedException">This method is called after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public void ClearAll() => this.ClearAll(false);

        /// <summary>Reset the position back to zero.</summary>
        /// <param name="fillZero">Determines whether the underlying buffer should be filled with zero before resetting position.</param>
        /// <exception cref="ObjectDisposedException">This method is called after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public void ClearAll(bool fillZero)
        {
            this.EnsureNotDisposed();
            if (this.pos != 0)
            {
                if (fillZero && this.arr != null && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(this.arr, 0, this.pos);
                }
                this.pos = 0;
            }
        }

        /// <summary>Removes the specific part of the underlying buffer.</summary>
        /// <param name="startIndex">The index to start removing data.</param>
        /// <param name="length">The amount of data to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="startIndex"/> cannot be less than zero.</para>
        /// <para><paramref name="length"/> cannot be less than zero.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="startIndex"/> is more than <seealso cref="WrittenCount"/>.</para>
        /// <para>Sum of <paramref name="startIndex"/> and <paramref name="length"/> is more than <seealso cref="WrittenCount"/>.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">This method is called after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public void Clear(int startIndex, int length)
        {
            this.EnsureNotDisposed();
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Parameter cannot be less than zero.");
            }
            else if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Parameter cannot be less than zero.");
            }
            else if (this.arr == null)
            {
                return;
            }
            else if (this.pos - startIndex < length )
            {
                throw new ArgumentException(nameof(startIndex), "Parameter cannot be more than the amount of written data.");
            }

            if (length > 0)
            {
                this.pos -= length;
                if (startIndex < this.pos)
                {
                    Array.Copy(this.arr, startIndex + length, this.arr, startIndex, this.pos - startIndex);
                }
                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(this.arr, this.pos, length);
                }
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">This property is accessed after <seealso cref="Dispose()"/> was called on this instance.</exception>
        public Memory<T> GetMemory(int sizeHint = 0) => this.GetMemory(false, sizeHint);

        /// <summary>Returns a <seealso cref="Memory{T}"/> to write to that is at least the requested size (specified by <paramref name="sizeHint"/>).</summary>
        /// <param name="fillZeroOnResize">Determines whether the old underlying buffer will be filled with zero after the buffer is resized.</param>
        /// <param name="sizeHint">The minimum length of the returned <seealso cref="Memory{T}"/>. If 0, a non-empty buffer is returned.</param>
        /// <returns>A <seealso cref="Memory{T}"/> of at least the size <paramref name="sizeHint"/>. If <paramref name="sizeHint"/> is 0, returns a non-empty buffer.</returns>
        /// <exception cref="OutOfMemoryException">The requested buffer size is not available.</exception>
        /// <exception cref="ObjectDisposedException">This method is called after <seealso cref="Dispose()"/> was called on this instance.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeHint"/> is less than 0.</exception>
        public Memory<T> GetMemory(bool fillZeroOnResize, int sizeHint = 0)
        {
            this.EnsureNotDisposed();
            if (sizeHint < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeHint), "Parameter cannot be less than zero.");
            }
            if (this.arr == null)
            {
                if (sizeHint == 0)
                {
                    sizeHint = GetMinSize();
                }
                if (this._pool == null)
                {
                    this.arr = new T[sizeHint];
                }
                else
                {
                    this.arr = this._pool.Rent(sizeHint);
                }
            }
            else
            {
                var free = this.FreeCapacity;
                if (sizeHint == 0)
                {
                    if (free == 0)
                    {
                        sizeHint = GetMinSize();
                    }
                    else
                    {
                        return new Memory<T>(this.arr, this.pos, free);
                    }
                }
                if (free == 0 || free < sizeHint)
                {
                    var oldArray = this.arr;
                    if ((oldArray.Length + sizeHint) > int.MaxValue)
                    {
                        throw new OutOfMemoryException();
                    }
                    if (this._pool == null)
                    {
                        if (fillZeroOnResize)
                        {
                            this.arr = new T[Math.Max(this.pos + sizeHint, this.pos + GetMinSize())];
                            Array.Copy(oldArray, this.arr, this.pos);
                            Array.Clear(oldArray, 0, this.pos);
                        }
                        else
                        {
                            Array.Resize(ref this.arr, Math.Max(this.pos + sizeHint, this.pos + GetMinSize()));
                        }
                    }
                    else
                    {
                        this.arr = this._pool.Rent(this.pos + sizeHint);
                        Array.Copy(oldArray, this.arr, this.pos);
                        this._pool.Return(this.arr, fillZeroOnResize);
                    }
                }
            }

            return new Memory<T>(this.arr, this.pos, this.arr.Length - this.pos);
        }

        private static int GetMinSize()
        {
            var t = typeof(T);
            if (t == typeof(byte))
            {
                return 4096;
            }
            else if (t.IsByRef)
            {
                return 32;
            }
            else
            {
                return 256;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="OutOfMemoryException">The requested buffer size is not available.</exception>
        /// <exception cref="ObjectDisposedException">This property is accessed after <seealso cref="Dispose()"/> was called on this instance.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sizeHint"/> is less than 0.</exception>
        public Span<T> GetSpan(int sizeHint = 0) => this.GetMemory(sizeHint).Span;

        private void EnsureNotDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(Bufferer<T>));
            }
        }

        /// <summary>Returns the underlying rented buffer to the pool if this instance is initialized with an <seealso cref="ArrayPool{T}"/>. Otherwise, does nothing.</summary>
        /// <remarks>Once disposed, any attempts to use this instance will throw <seealso cref="ObjectDisposedException"/>.</remarks>
        public void Dispose()
        {
            if (this.disposed) return;
            this.disposed = true;
            GC.SuppressFinalize(this);
            this.Dispose(true);
        }

        /// <summary>When invoked, returns the underlying rented buffer to the pool.</summary>
        /// <param name="disposing">
        /// <para>True when this method is invoked via <seealso cref="Dispose()"/>.</para>
        /// <para>False when this method is invoked via GC finalizer.</para>
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.arr is T[] array)
            {
                this.arr = null;
                this._pool?.Return(array);
            }
        }

        /// <summary>Finalizer</summary>
        ~Bufferer()
        {
            this.Dispose(false);
        }
    }
}
