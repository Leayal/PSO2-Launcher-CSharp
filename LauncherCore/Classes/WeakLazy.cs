﻿using System;

#nullable enable
namespace Leayal.PSO2Launcher.Core.Classes
{
    /// <summary>Provides support for lazy initialization where the object can be collected by GC and re-create again when needed.</summary>
    /// <typeparam name="T">The type of object that is being lazily initialized.</typeparam>
    /// <remarks>This class is not thread-safe.</remarks>
    class WeakLazy<T> where T : class
    {
        private readonly WeakReference<T?> _reference;
        private readonly Func<T> factory;

        /// <summary>Initializes a new instance. When lazy initialization occurs, the specified initialization function is used.</summary>
        /// <param name="factory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is null.</exception>
        public WeakLazy(Func<T> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            this.factory = factory;
            this._reference = new WeakReference<T?>(null, false);
        }

        /// <summary>Gets a value that indicates whether a value has been created for this instance.</summary>
        public bool IsValueCreated => this._reference.TryGetTarget(out var item) && item != null;

        /// <summary>Gets the lazily initialized value of the current instance.</summary>
        public T Value
        {
            get
            {
                if (!this._reference.TryGetTarget(out var item) || item == null)
                {
                    item = this.factory.Invoke();
                    this._reference.SetTarget(item);
                }
                return item;
            }
        }
    }
}
#nullable restore
