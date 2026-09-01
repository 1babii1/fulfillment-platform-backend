namespace Ordering;

/// <summary>
///     Fractional indexing algorithm for generating lexicographically sortable order keys.
///     Ported from https://github.com/rocicorp/fractional-indexing (CC0 license).
///     Base-62 encoding: 0-9 A-Z a-z.
/// </summary>
public static class FractionalIndexing
{
    public const string Base62Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private static readonly string SmallestInteger = "A" + new string(Base62Digits[0], 26);

    /// <summary>
    ///     Generates a single order key between two existing keys.
    ///     Pass null for <paramref name="a" /> to generate a key before all others.
    ///     Pass null for <paramref name="b" /> to generate a key after all others.
    ///     Both null — generates the initial key "a0".
    /// </summary>
    /// <param name="a">Lower bound key, or null for no lower bound.</param>
    /// <param name="b">Upper bound key, or null for no upper bound.</param>
    public static string GenerateKeyBetween(string? a, string? b)
    {
        string digits = Base62Digits;

        if (a is not null)
        {
            ValidateOrderKey(a);
        }

        if (b is not null)
        {
            ValidateOrderKey(b);
        }

        if (a is not null && b is not null && string.CompareOrdinal(a, b) >= 0)
        {
            throw new ArgumentException($"Order key '{a}' must be less than '{b}'.");
        }

        if (a is null)
        {
            if (b is null)
            {
                return "a" + digits[0];
            }

            string ib = GetIntegerPart(b);
            string fb = b[ib.Length..];

            if (ib == SmallestInteger)
            {
                return ib + Midpoint(string.Empty, fb, digits);
            }

            if (string.CompareOrdinal(ib, b) < 0)
            {
                return ib;
            }

            string? decremented = DecrementInteger(ib, digits);
            if (decremented is null)
            {
                throw new InvalidOperationException("Cannot decrement order key integer below minimum.");
            }

            return decremented;
        }

        if (b is null)
        {
            string ia = GetIntegerPart(a);
            string fa = a[ia.Length..];
            string? incremented = IncrementInteger(ia, digits);

            return incremented ?? ia + Midpoint(fa, null, digits);
        }

        // Both non-null
        string iaB = GetIntegerPart(a);
        string faB = a[iaB.Length..];
        string ibB = GetIntegerPart(b);
        string fbB = b[ibB.Length..];

        if (iaB == ibB)
        {
            return iaB + Midpoint(faB, fbB, digits);
        }

        string? inc = IncrementInteger(iaB, digits);

        if (inc is not null && string.CompareOrdinal(inc, b) < 0)
        {
            return inc;
        }

        return iaB + Midpoint(faB, null, digits);
    }

    /// <summary>
    ///     Generates <paramref name="n" /> evenly-spaced keys between <paramref name="a" /> and <paramref name="b" />.
    /// </summary>
    /// <param name="a">Lower bound key, or null for no lower bound.</param>
    /// <param name="b">Upper bound key, or null for no upper bound.</param>
    /// <param name="n">Number of keys to generate.</param>
    public static string[] GenerateNKeysBetween(string? a, string? b, int n)
    {
        if (n == 0)
        {
            return [];
        }

        if (n == 1)
        {
            return [GenerateKeyBetween(a, b)];
        }

        if (b is null)
        {
            string c = GenerateKeyBetween(a, b);
            string[] result = new string[n];
            result[0] = c;

            for (int i = 1; i < n; i++)
            {
                c = GenerateKeyBetween(c, b);
                result[i] = c;
            }

            return result;
        }

        if (a is null)
        {
            string c = GenerateKeyBetween(a, b);
            string[] result = new string[n];
            result[n - 1] = c;

            for (int i = n - 2; i >= 0; i--)
            {
                c = GenerateKeyBetween(a, c);
                result[i] = c;
            }

            return result;
        }

        int mid = n / 2;
        string midKey = GenerateKeyBetween(a, b);
        string[] left = GenerateNKeysBetween(a, midKey, mid);
        string[] right = GenerateNKeysBetween(midKey, b, n - mid - 1);

        string[] combined = new string[n];
        left.CopyTo(combined, 0);
        combined[mid] = midKey;
        right.CopyTo(combined, mid + 1);

        return combined;
    }

    internal static string Midpoint(string a, string? b, string digits)
    {
        char zero = digits[0];

        if (b is not null && string.CompareOrdinal(a, b) >= 0)
        {
            throw new ArgumentException($"Midpoint requires a < b, got '{a}' >= '{b}'.");
        }

        if (b is not null)
        {
            int n = 0;
            while (n < a.Length && n < b.Length && a[n] == b[n])
            {
                n++;
            }

            // Also skip matching zeros in a (padding)
            if (n == a.Length)
            {
                while (n < b.Length && b[n] == zero)
                {
                    n++;
                }
            }

            if (n > 0)
            {
                return b[..n] + Midpoint(a.Length > n ? a[n..] : string.Empty, b[n..], digits);
            }
        }

        int digitA = a.Length > 0 ? digits.IndexOf(a[0], StringComparison.Ordinal) : 0;
        int digitB = b is not null ? digits.IndexOf(b[0], StringComparison.Ordinal) : digits.Length;

        if (digitB - digitA > 1)
        {
            int midDigit = (int)Math.Round(0.5 * (digitA + digitB));
            return digits[midDigit].ToString();
        }

        // Consecutive digits
        if (b is not null && b.Length > 1)
        {
            return b[..1];
        }

        return digits[digitA] + Midpoint(a.Length > 1 ? a[1..] : string.Empty, null, digits);
    }

    internal static string? IncrementInteger(string x, string digits)
    {
        ValidateInteger(x);

        char head = x[0];
        char[] digs = x[1..].ToCharArray();
        bool carry = true;

        for (int i = digs.Length - 1; carry && i >= 0; i--)
        {
            int d = digits.IndexOf(digs[i], StringComparison.Ordinal) + 1;

            if (d == digits.Length)
            {
                digs[i] = digits[0];
            }
            else
            {
                digs[i] = digits[d];
                carry = false;
            }
        }

        if (carry)
        {
            if (head == 'Z')
            {
                return "a" + digits[0];
            }

            if (head == 'z')
            {
                return null;
            }

            char h = (char)(head + 1);
            string digsStr = new(digs);

            if (h > 'a')
            {
                // 'a'..'y' → 'b'..'z': integer length grows, append a digit
                return h + digsStr + digits[0];
            }

            // 'A'..'Y' → 'B'..'Z': integer length shrinks, remove last digit
            return h + digsStr[..^1];
        }

        return head + new string(digs);
    }

    internal static string? DecrementInteger(string x, string digits)
    {
        ValidateInteger(x);

        char head = x[0];
        char[] digs = x[1..].ToCharArray();
        bool borrow = true;

        for (int i = digs.Length - 1; borrow && i >= 0; i--)
        {
            int d = digits.IndexOf(digs[i], StringComparison.Ordinal) - 1;

            if (d == -1)
            {
                digs[i] = digits[^1];
                borrow = true;
            }
            else
            {
                digs[i] = digits[d];
                borrow = false;
            }
        }

        if (borrow)
        {
            if (head == 'a')
            {
                return "Z" + digits[^1];
            }

            if (head == 'A')
            {
                return null;
            }

            char h = (char)(head - 1);
            string digsStr = new(digs);

            if (h < 'Z')
            {
                // 'Z'..'B' → 'Y'..'A': integer length grows, append a digit
                return h + digsStr + digits[^1];
            }

            // 'z'..'b' → 'y'..'a': integer length shrinks, remove last digit
            return h + digsStr[..^1];
        }

        return head + new string(digs);
    }

    private static void ValidateOrderKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Order key must not be empty.");
        }

        string intPart = GetIntegerPart(key);

        if (intPart == SmallestInteger)
        {
            throw new ArgumentException($"Order key has invalid integer part: '{intPart}'.");
        }

        string fracPart = key[intPart.Length..];

        if (fracPart.Length > 0 && fracPart[^1] == Base62Digits[0])
        {
            throw new ArgumentException($"Order key fractional part must not end with '{Base62Digits[0]}'.");
        }
    }

    private static void ValidateInteger(string integer)
    {
        if (integer.Length != GetIntegerLength(integer[0]))
        {
            throw new ArgumentException($"Invalid integer part of order key: '{integer}'.");
        }
    }

    private static string GetIntegerPart(string key)
    {
        int length = GetIntegerLength(key[0]);

        if (length > key.Length)
        {
            throw new ArgumentException($"Order key '{key}' is too short for integer length {length}.");
        }

        return key[..length];
    }

    private static int GetIntegerLength(char head)
    {
        if (head >= 'a' && head <= 'z')
        {
            return head - 'a' + 2;
        }

        if (head >= 'A' && head <= 'Z')
        {
            return 'Z' - head + 2;
        }

        throw new ArgumentException($"Invalid order key head character: '{head}'.");
    }
}