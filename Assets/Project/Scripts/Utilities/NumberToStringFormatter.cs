using UnityEngine;
using System;

namespace GeneisIRL 
{
    public static class NumberFormatter
    {
        /// <summary>
        /// Formats a float value by rounding it to the specified number of decimal places and removing trailing zeros.
        /// </summary>
        /// <param name="value">The float value to format.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places.</param>
        /// <returns>A formatted string representation of the number.</returns>
        public static string FormatNumber(float value, int decimalPlaces)
        {
            // Convert float to decimal for more precise rounding
            return FormatNumber((decimal)value, decimalPlaces);
        }

        /// <summary>
        /// Formats a double value by rounding it to the specified number of decimal places and removing trailing zeros.
        /// </summary>
        /// <param name="value">The double value to format.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places.</param>
        /// <returns>A formatted string representation of the number.</returns>
        public static string FormatNumber(double value, int decimalPlaces)
        {
            // Convert double to decimal for rounding
            return FormatNumber((decimal)value, decimalPlaces);
        }

        /// <summary>
        /// Formats a decimal value by rounding it to the specified number of decimal places and removing trailing zeros.
        /// </summary>
        /// <param name="value">The decimal value to format.</param>
        /// <param name="decimalPlaces">The maximum number of decimal places.</param>
        /// <returns>A formatted string representation of the number.</returns>
        public static string FormatNumber(decimal value, int decimalPlaces)
        {
            // Round the value to the specified number of decimal places.
            // Here we use MidpointRounding.AwayFromZero so that .005 rounds to .01, for example.
            decimal rounded = Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);

            // The "G29" format specifier ensures that up to 29 significant digits are output and that no unnecessary trailing zeros appear.
            return rounded.ToString("G29");
        }
    }



}