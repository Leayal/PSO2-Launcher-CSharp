﻿namespace Leayal.PSO2Launcher.Core.Classes.PSO2.GameConfig
{
    readonly struct MonitorCountWrapper
    {
        public readonly int DisplayNo { get; }

        public MonitorCountWrapper(int no_)
        {
            this.DisplayNo = no_;
        }
    }
}
