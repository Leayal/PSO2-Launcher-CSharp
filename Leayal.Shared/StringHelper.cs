using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Leayal.Shared
{
    public static class StringHelper
    {
        /// <summary>Creates a <seealso cref="Predicate{T}"/> which determines whether a string matches a pattern.</summary>
        /// <param name="pattern">
        /// <para>Any string expressions conforming to the pattern-matching conventions <see href="https://learn.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator#pattern-options">described in details on learn.microsoft.com</see>.</para>
        /// <para>However, a slight difference is that <paramref name="pattern"/> cannot be <see langword="null"/> or an empty string.</para>
        /// </param>
        /// <param name="isCaseSensitive">Determins whether the pattern matching will be case sensitive or not.</param>
        /// <returns>A <seealso cref="Predicate{T}"/> which determines whether a string matched the <paramref name="pattern"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pattern"/> is an empty string.</exception>
        public static Predicate<object> MakePredicate_MatchByPattern<T>(string pattern, bool isCaseSensitive) where T : IStringComparable
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            else if (pattern.Length == 0) throw new ArgumentException(null, nameof(pattern));

            var wrapper = new Comparer_MatchByPattern<T>(pattern, isCaseSensitive);
            return new Predicate<object>(wrapper.LikeOp);
        }

        /// <summary>Creates a <seealso cref="Predicate{T}"/> which determines whether a string contains the given literal text.</summary>
        /// <param name="text">A literal string. Cannot be <see langword="null"/> or an empty string.</param>
        /// <param name="isCaseSensitive">Determins whether the text matching will be case sensitive or not.</param>
        /// <returns>A <seealso cref="Predicate{T}"/> which determines whether a string contains the <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> is an empty string.</exception>
        public static Predicate<object> MakePredicate_ContainsLiteral<T>(ReadOnlyMemory<char> text, bool isCaseSensitive) where T : IStringComparable
        {
            if (text.IsEmpty) throw new ArgumentException(null, nameof(text));

            var wrapper = new Comparer_MatchLiteral<T>(text, isCaseSensitive);
            return new Predicate<object>(wrapper.ContainsOp);
        }

        private readonly struct Comparer_MatchLiteral<T>
        {
            private readonly ReadOnlyMemory<char> text;
            private readonly StringComparison method;

            public Comparer_MatchLiteral(in ReadOnlyMemory<char> text, bool isCaseSensitive) : this()
            {
                this.text = text;
                this.method = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public bool ContainsOp(object? obj)
            {
                if (obj is string str)
                    return MemoryExtensions.Contains(str.AsSpan(), this.text.Span, this.method);
                else if (obj is ReadOnlyMemory<char> mem)
                    return MemoryExtensions.Contains(mem.Span, this.text.Span, this.method);
                else if (obj is IStringComparable toStringAble)
                    return MemoryExtensions.Contains(toStringAble.GetComparableStringRegion().Span, this.text.Span, this.method);
                else
                    return false;
            }
        }

        private readonly struct Comparer_MatchByPattern<T> where T : IStringComparable
        {
            private readonly string pattern;
            private readonly Microsoft.VisualBasic.CompareMethod method;

            public Comparer_MatchByPattern(string pattern, bool isCaseSensitive) : this()
            {
                this.pattern = pattern;
                this.method = isCaseSensitive ? Microsoft.VisualBasic.CompareMethod.Binary : Microsoft.VisualBasic.CompareMethod.Text;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            public bool LikeOp(object? obj)
            {
                if (obj is string str)
                    return Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(str, this.pattern, this.method);
                else if (obj is ReadOnlyMemory<char> mem)
                    return Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(new string(mem.Span), this.pattern, this.method);
                else if (obj is IStringComparable toStringAble)
                    return Microsoft.VisualBasic.CompilerServices.LikeOperator.LikeString(new string(toStringAble.GetComparableStringRegion().Span), this.pattern, this.method);
                else
                    return false;
            }
        }

        /// <summary>Ensures an object can be compared by string.</summary>
        public interface IStringComparable
        {
            /// <summary>When override, provides implementation to get a part a string or a whole string, which will be used for comparison.</summary>
            /// <returns>A part of a string or a whole string.</returns>
            public ReadOnlyMemory<char> GetComparableStringRegion();
        }
    }
}
