using System.Windows.Media;

namespace SimpleNotesApp;

public static class ColorHelper
{
    public static Color Lighten(Color color, double amount = 0.25)
    {
        ToHsl(color.R, color.G, color.B, out double h, out double s, out double l);
        l = Math.Min(1.0, l + amount);
        FromHsl(h, s, l, out byte r, out byte g, out byte b);
        return Color.FromArgb(color.A, r, g, b);
    }

    public static Color Darken(Color color, double amount = 0.15)
    {
        ToHsl(color.R, color.G, color.B, out double h, out double s, out double l);
        l = Math.Max(0.0, l - amount);
        FromHsl(h, s, l, out byte r, out byte g, out byte b);
        return Color.FromArgb(color.A, r, g, b);
    }

    public static Color FromHex(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 6)
            return Color.FromRgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        if (hex.Length == 8)
            return Color.FromArgb(
                Convert.ToByte(hex[..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16),
                Convert.ToByte(hex[6..8], 16));
        return Colors.Black;
    }

    public static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static void ToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;
        double max = Math.Max(rn, Math.Max(gn, bn));
        double min = Math.Min(rn, Math.Min(gn, bn));
        l = (max + min) / 2.0;

        if (max == min)
        {
            h = s = 0.0;
        }
        else
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
            h = max switch
            {
                _ when rn == max => (gn - bn) / d + (gn < bn ? 6.0 : 0.0),
                _ when gn == max => (bn - rn) / d + 2.0,
                _ => (rn - gn) / d + 4.0
            };
            h /= 6.0;
        }
    }

    private static void FromHsl(double h, double s, double l, out byte r, out byte g, out byte b)
    {
        if (s == 0.0)
        {
            byte v = (byte)Math.Round(l * 255.0);
            r = g = b = v;
            return;
        }

        double q = l < 0.5 ? l * (1.0 + s) : l + s - l * s;
        double p = 2.0 * l - q;
        r = HueToRgb(p, q, h + 1.0 / 3.0);
        g = HueToRgb(p, q, h);
        b = HueToRgb(p, q, h - 1.0 / 3.0);
    }

    private static byte HueToRgb(double p, double q, double t)
    {
        if (t < 0.0) t += 1.0;
        if (t > 1.0) t -= 1.0;
        if (t < 1.0 / 6.0) return (byte)Math.Round((p + (q - p) * 6.0 * t) * 255.0);
        if (t < 1.0 / 2.0) return (byte)Math.Round(q * 255.0);
        if (t < 2.0 / 3.0) return (byte)Math.Round((p + (q - p) * (2.0 / 3.0 - t) * 6.0) * 255.0);
        return (byte)Math.Round(p * 255.0);
    }
}
