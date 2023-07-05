using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace AtlonaOme
{
    public static class StringExtensions
    {
        /// <summary>
        /// Overload for Contains that allows setting an explicit String Comparison
        /// </summary>
        /// <param name="source">Source String</param>
        /// <param name="toCheck">String to check in Source String</param>
        /// <param name="comp">Comparison parameters</param>
        /// <returns>true of string contains "toCheck"</returns>
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            if (string.IsNullOrEmpty(source)) return false;
            return source.IndexOf(toCheck, comp) >= 0;
        }

        /// <summary>
        /// Performs TrimStart() and TrimEnd() on source string
        /// </summary>
        /// <param name="source">String to Trim</param>
        /// <returns>Trimmed String</returns>
        public static string TrimAll(this string source)
        {
            return string.IsNullOrEmpty(source) ? string.Empty : source.TrimStart().TrimEnd();
        }

        /// <summary>
        /// Performs TrimStart(chars char[]) and TrimEnd(chars char[]) on source string.
        /// </summary>
        /// <param name="source">String to Trim</param>
        /// <param name="chars">Char Array to trim from string</param>
        /// <returns>Trimmed String</returns>
        public static string TrimAll(this string source, char[] chars)
        {
            return string.IsNullOrEmpty(source) ? string.Empty : source.TrimStart(chars).TrimEnd(chars);
        }


    }
}