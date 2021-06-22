using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2Launcher.Core.Classes.PSO2.DataTypes
{
    readonly struct PSO2Version : IComparable<PSO2Version>, IEquatable<PSO2Version>
    {
        public readonly int Version;
        public readonly int ReleaseCandidate;

        public PSO2Version(int version, int rc)
        {
            this.Version = version;
            this.ReleaseCandidate = rc;
        }

        public static bool TryParse(in string versionString, out PSO2Version value) => TryParse(versionString.AsSpan(), out value);

        public static bool TryParse(ReadOnlySpan<char> versionString, out PSO2Version value)
        {
            // v70000_rc_148
            // I guess nothing beat Regex....
            // Or not

            // WTF I am doing
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
                    value = new PSO2Version(versionNumber, 0);
                    return true;
                }
            }
            else
            {
                if (int.TryParse(buffer.Slice(0, index), System.Globalization.NumberStyles.Integer, null, out var versionNumber)
                    && int.TryParse(buffer.Slice(index + (comparand.Length - 1)), System.Globalization.NumberStyles.Integer, null, out var versionRC))
                {
                    value = new PSO2Version(versionNumber, versionRC);
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static bool TrySafeParse(in string versionString, out PSO2Version value)
        {
            var trimmed = versionString.AsSpan().Trim(); // For safety, trim it
            var spaceIndex = trimmed.IndexOfAny(new char[] { ' ', '\t' });
            if (spaceIndex != -1)
            {
                trimmed = trimmed.Slice(0, spaceIndex);
            }

            return PSO2Version.TryParse(trimmed, out value);
        }

        /// <summary>Compares this instance to a specified <see cref="PSO2Version"/> and returns an indication of their relative values.</summary>
        /// <returns>
        /// <para>A signed number indicating the relative values of this instance and value.</para>
        /// <para>Return Value – Description</para>
        /// <para><b>Less than zero</b> – This instance is less than <paramref name="other"/>.</para>
        /// <para><b>Zero</b> – This instance is equal to <paramref name="other"/>.</para>
        /// <para><b>Greater than zero</b> – This instance is greater than <paramref name="other"/>.</para>
        /// </returns>
        public int CompareTo(PSO2Version other)
        {
            var verCompare = this.Version.CompareTo(other.Version);
            if (verCompare == 0)
            {
                return this.ReleaseCandidate.CompareTo(other.ReleaseCandidate);
            }
            else
            {
                return verCompare;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is PSO2Version ver)
            {
                return this.Equals(ver);
            }
            return false;
        }

        public override int GetHashCode() => this.Version.GetHashCode() ^ this.ReleaseCandidate.GetHashCode();

        public override string ToString() => $"v{this.Version}_rc_{this.ReleaseCandidate}";

        public bool Equals(PSO2Version other) => (this.Version.Equals(other.Version) && this.ReleaseCandidate.Equals(other.ReleaseCandidate));

        public static bool operator ==(PSO2Version left, PSO2Version right) => ((left.Version == right.Version) && (left.ReleaseCandidate == right.ReleaseCandidate));

        public static bool operator !=(PSO2Version left, PSO2Version right) => ((left.Version != right.Version) || (left.ReleaseCandidate != right.ReleaseCandidate));

        public static bool operator <(PSO2Version left, PSO2Version right) => (left.CompareTo(right) < 0);

        public static bool operator >(PSO2Version left, PSO2Version right) => (left.CompareTo(right) > 0);
    }
}
