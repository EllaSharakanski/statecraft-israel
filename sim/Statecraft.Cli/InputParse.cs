using System.Globalization;

namespace Statecraft.Cli
{
    /// <summary>Culture-invariant parsing for text front ends; failures are values, never exceptions.</summary>
    public static class InputParse
    {
        public static bool TryDouble(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);

        public static bool TryInt(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        public static bool TryULong(string text, out ulong value) =>
            ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
