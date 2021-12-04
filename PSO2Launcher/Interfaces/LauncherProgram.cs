﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Interfaces
{
    /// <summary>A base implementation which can be extended to form a single-instance main program.</summary>
    public abstract class LauncherProgram : ILauncherProgram
    {
        private readonly bool haswpf, haswinform;
        private int hasRunFirstTime;

        public bool HasWPF => this.haswpf;

        public bool HasWinForm => this.haswinform;

        public event EventHandler? Initialized;

        protected LauncherProgram(bool hasWinform, bool hasWPF)
        {
            this.haswinform = hasWinform;
            this.haswpf = hasWPF;
            this.hasRunFirstTime = 0;
        }

        public int Exit()
        {
            if (Interlocked.CompareExchange(ref this.hasRunFirstTime, 0, 1) == 1)
            {
                return this.OnExit();
            }
            else
            {
                return 0;
            }
        }

        public virtual void Run(string[] args)
        {
            if (Interlocked.CompareExchange(ref this.hasRunFirstTime, 1, 0) == 0)
            {
                this.OnFirstInstance(args);
            }
            else
            {
                this.OnSubsequentInstance(args);
            }
        }

        /// <summary>Raises the <seealso cref="Initialized"/> event.</summary>
        protected virtual void OnInitialized()
        {
            this.Initialized?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>When overrides, logic code for the first instance of the program.</summary>
        protected abstract void OnFirstInstance(string[] args);

        /// <summary>When overrides, logic code for the instances after the first one of the program.</summary>
        protected abstract void OnSubsequentInstance(string[] args);

        /// <summary>When overrides, logic code to exit/terminate the first instance.</summary>
        protected abstract int OnExit();
    }
}
