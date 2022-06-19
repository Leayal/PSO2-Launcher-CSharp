using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;

namespace Leayal.PSO2Launcher.Toolbox.Windows
{
    sealed class AccountData : DependencyObject, IEquatable<AccountData>
    {
        public readonly long AccountID;
        private readonly HashSet<string> names;
        private readonly StringBuilder sb;

        // private static readonly DependencyPropertyKey NamePropertyKey = DependencyProperty.RegisterReadOnly("Name", typeof(string), typeof(AccountData), new PropertyMetadata(string.Empty));
        // public static readonly DependencyProperty NameProperty = NamePropertyKey.DependencyProperty;

        // public string Name => (string)this.GetValue(NameProperty);

        private static readonly DependencyPropertyKey NamesOnlyPropertyKey = DependencyProperty.RegisterReadOnly("NamesOnly", typeof(string), typeof(AccountData), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty NamesOnlyProperty = NamesOnlyPropertyKey.DependencyProperty;

        public string NamesOnly => (string)this.GetValue(NamesOnlyProperty);

        public long ID => this.AccountID;

        public static readonly DependencyProperty AlphaReactorCountProperty = DependencyProperty.Register("AlphaReactorCount", typeof(int), typeof(AccountData), new PropertyMetadata(0));
        public int AlphaReactorCount
        {
            get => (int)this.GetValue(AlphaReactorCountProperty);
            set => this.SetValue(AlphaReactorCountProperty, value);
        }

        public static readonly DependencyProperty StellarSeedCountProperty = DependencyProperty.Register("StellarSeedCount", typeof(int), typeof(AccountData), new PropertyMetadata(0));

        public int StellarSeedCount
        {
            get => (int)this.GetValue(StellarSeedCountProperty);
            set => this.SetValue(StellarSeedCountProperty, value);
        }

        public static readonly DependencyProperty SnowkCountProperty = DependencyProperty.Register("SnowkCount", typeof(int), typeof(AccountData), new PropertyMetadata(0));

        public int SnowkCount
        {
            get => (int)this.GetValue(SnowkCountProperty);
            set => this.SetValue(SnowkCountProperty, value);
        }

        public static readonly DependencyProperty BlizzardiumCountProperty = DependencyProperty.Register("BlizzardiumCount", typeof(int), typeof(AccountData), new PropertyMetadata(0));

        public int BlizzardiumCount
        {
            get => (int)this.GetValue(BlizzardiumCountProperty);
            set => this.SetValue(BlizzardiumCountProperty, value);
        }

        public AccountData(long characterID)
        {
            this.sb = new StringBuilder();
            this.names = new HashSet<string>();
            this.AccountID = characterID;
        }

        public void AddName(string name)
        {
            if (this.names.Add(name))
            {
                this.sb.Clear();
                bool isFirst = true;
                foreach (var n in this.names)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        this.sb.Append(n);
                    }
                    else
                    {
                        this.sb.Append(", ").Append(n);
                    }
                }
                this.SetValue(NamesOnlyPropertyKey, this.sb.ToString());
                // this.sb.Append(" (Account ID: ").Append(this.AccountID).Append(')');
                // this.SetValue(NamePropertyKey, this.sb.ToString());
            }
        }

        public bool Equals(AccountData? other)
        {
            if (other is null) return false;
            else return (this.AccountID == other.AccountID);
        }
    }
}
