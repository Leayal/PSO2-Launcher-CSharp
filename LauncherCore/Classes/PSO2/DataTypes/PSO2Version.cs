using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    public readonly struct PSO2Version : IComparable<PSO2Version>, IEquatable<PSO2Version>
    {
        public readonly int Version;
        public readonly string ReleaseCandidate;

        public PSO2Version(int version, string rc)
        {
            this.Version = version;
            this.ReleaseCandidate = rc;
        }

        public static bool TryParse(string versionString, out PSO2Version value)
        {
            if (versionString == null)
            {
                value = default;
                return false;
            }
            else
            {
                return TryParse(versionString.AsSpan(), out value);
            }
        }

        public static bool TryParse(ReadOnlySpan<char> versionString, out PSO2Version value)
        {
            // v70000_rc_148
            // I guess nothing beat Regex....
            // Or not

            // WTF I am doing
            if (versionString.Length == 0 || versionString.IsWhiteSpace())
            {
                value = default;
                return false;
            }

            var comparand = "v_rc_".AsSpan();
            ReadOnlySpan<char> buffer;
            if (versionString[0] == comparand[0])
            {
                buffer = versionString.Slice(1);
            }
            else if (char.IsDigit(versionString[0]))
            {
                buffer = versionString;
            }
            else
            {
                value = default;
                return false;
            }

            var index = buffer.IndexOf(comparand.Slice(1));
            if (index == -1)
            {
                if (int.TryParse(buffer, System.Globalization.NumberStyles.Integer, null, out var versionNumber))
                {
                    value = new PSO2Version(versionNumber, string.Empty);
                    return true;
                }
            }
            else
            {
                if (int.TryParse(buffer.Slice(0, index), System.Globalization.NumberStyles.Integer, null, out var versionNumber))
                {
                    value = new PSO2Version(versionNumber, new string(buffer.Slice(index + (comparand.Length - 1))));
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>Compares this instance to a specified <see cref="PSO2Version"/> and returns an indication of their relative values.</summary>
        /// <returns>
        /// <para>A signed number indicating the relative values of this instance and value.</para>
        /// <para>Return Value – Description</para>
        /// <para><b>Less than zero</b> – This instance is less than <paramref name="other"/>.</para>
        /// <para><b>Zero</b> – This instance is equal to <paramref name="other"/>.</para>
        /// <para><b>Greater than zero</b> – This instance is greater than <paramref name="other"/>.</para>
        /// </returns>
        public readonly int CompareTo(PSO2Version other)
        {
            var verCompare = this.Version.CompareTo(other.Version);
            if (verCompare == 0)
            {
                if (int.TryParse(this.ReleaseCandidate, out var thisRC) && int.TryParse(other.ReleaseCandidate, out var otherRC))
                {
                    return thisRC.CompareTo(otherRC);
                }
                else
                {
                    thisRC = TakeDigitOnly(in this.ReleaseCandidate);
                    otherRC = TakeDigitOnly(in other.ReleaseCandidate);
                    return thisRC.CompareTo(otherRC);
                }
            }
            else
            {
                return verCompare;
            }
        }

        private static int TakeDigitOnly(in string str)
        {
            var span = str.AsSpan();
            var sb = new StringBuilder(span.Length);
            for (int i = 0; i < span.Length; i++)
            {
                if (char.IsDigit(span[i]))
                {
                    sb.Append(span[i]);
                }
            }

            return int.Parse(sb.ToString());
        }

        public override readonly bool Equals(object? obj)
        {
            if (obj is PSO2Version ver)
            {
                return this.Equals(ver);
            }
            return false;
        }

        public readonly override int GetHashCode() => HashCode.Combine(this.Version, this.ReleaseCandidate);

        public readonly override string ToString() => $"v{this.Version}_rc_{this.ReleaseCandidate}";

        public readonly bool Equals(PSO2Version other) => (this.Version.Equals(other.Version) && this.ReleaseCandidate.Equals(other.ReleaseCandidate));

        public static bool operator ==(PSO2Version left, PSO2Version right) => ((left.Version == right.Version) && (left.ReleaseCandidate == right.ReleaseCandidate));

        public static bool operator !=(PSO2Version left, PSO2Version right) => ((left.Version != right.Version) || (left.ReleaseCandidate != right.ReleaseCandidate));

        public static bool operator <(PSO2Version left, PSO2Version right) => (left.CompareTo(right) < 0);

        public static bool operator >(PSO2Version left, PSO2Version right) => (left.CompareTo(right) > 0);
    }
}
