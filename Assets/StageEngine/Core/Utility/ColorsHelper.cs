using UnityEngine;

namespace StageEngine.Core.Utility
{
    public static class ColorsHelper
    {
        /// <summary>
        /// Converts a Unity Color to a hex string
        /// </summary>
        /// <param name="color">The Unity Color to convert</param>
        /// <param name="includeAlpha">Whether to include alpha channel in the hex string</param>
        /// <returns>Hex string (e.g., "#FF0000" or "#FF0000FF" with alpha)</returns>
        public static string ColorToHex(Color color, bool includeAlpha = false)
        {
            Color32 c = color;

            if (includeAlpha)
            {
                return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
            }
            else
            {
                return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
            }
        }

        /// <summary>
        /// Converts a hex string to a Unity Color
        /// </summary>
        /// <param name="hex">Hex string (with or without #, supports 3, 6, and 8 character formats)</param>
        /// <returns>Unity Color object</returns>
        public static Color HexToColor(string hex)
        {
            // Remove # if present
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            // Handle different hex formats
            switch (hex.Length)
            {
                case 3: // RGB shorthand (e.g., "F0A")
                    hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
                    break;
                case 6: // RRGGBB
                        // Already in correct format
                    break;
                case 8: // RRGGBBAA
                        // Already in correct format
                    break;
                default:
                    Debug.LogError($"Invalid hex color format: {hex}");
                    return Color.white;
            }

            try
            {
                byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
                byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
                byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
                byte a = 255; // Default alpha

                // Parse alpha if present
                if (hex.Length == 8)
                {
                    a = System.Convert.ToByte(hex.Substring(6, 2), 16);
                }

                return new Color32(r, g, b, a);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse hex color '{hex}': {e.Message}");
                return Color.white;
            }
        }
    }
}
