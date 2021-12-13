using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Leayal.Shared
{
    /// <summary>A base interface class to manage deferred objects which can be disable/enable.</summary>
    public abstract class ActivationBasedObject
    {
        private int activationcount;

        /// <summary>Applies default values to the current instance.</summary>
        /// <remarks>Default state of the activation is inactive.</remarks>
        protected ActivationBasedObject()
        {
            this.activationcount = 0;
        }

        /// <summary>Requests the object to be active.</summary>
        /// <returns>Returns true if the caller is okay to be in active. Otherwise false.</returns>
        protected virtual bool RequestActive()
        {
            if (Interlocked.Increment(ref this.activationcount) == 1)
            {
                this.OnActivation();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Gets a boolean determine whether the current instance is in active or not.</summary>
        public virtual bool IsCurrentlyActive => (Interlocked.Or(ref this.activationcount, 0) != 0);

        /// <summary>Requests the object to be inactive.</summary>
        /// <returns>Returns true if the caller is okay to stop being active. Otherwise false.</returns>
        protected virtual bool RequestDeactive()
        {
            if (Interlocked.Decrement(ref this.activationcount) == 0)
            {
                this.OnDeactivation();
                return true;
            }
            else
            {
                Interlocked.CompareExchange(ref this.activationcount, 0, -1);
                return false;
            }
        }

        /// <summary>When override, execution code when the instance becomes active from inactive.</summary>
        protected abstract void OnActivation();

        /// <summary>When override, execution code when the instance becomes inactive from active.</summary>
        protected abstract void OnDeactivation();
    }
}
