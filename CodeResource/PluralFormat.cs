using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeResource
{
    /// <summary>
    /// Provides extended string formatting for pluralized placeholders.<br/>
    /// For example: "You have {0:apple;apples} left"
    /// </summary>
    /// <example>
    /// Usage:
    /// <code language="cs">
    /// String.Format(new PluralFormatProvider(), "You have {0:life;lives} left, {1:apple;apples} and {2:eye;eyes}.", 1, 0, 2);
    /// </code>
    /// </example>
    public class PluralFormatProvider : IFormatProvider, ICustomFormatter
    {
        public static PluralFormatProvider Default { get; } = new PluralFormatProvider();

        public object GetFormat(Type formatType)
        {
            return this;
        }

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (String.IsNullOrWhiteSpace(format))
                return arg.ToString();

            string[] forms = format.Split(';');
            if (arg is int integer)
            {
                int form = integer == 1 ? 0 : 1;
                return /*integer.ToString() + " " +*/ forms[form].Replace("$", integer.ToString());
            }
            if (arg is double d)
            {
                int form = d == 1 ? 0 : 1;
                return /*d.ToString() + " " +*/ forms[form].Replace("$", d.ToString());
            }
            return String.Format("{0:" + format + "}", arg);
        }
    }

    public static class PluralizationExtension
    {
        /// <summary>
        /// Provides extended string formatting for pluralized placeholders.<br/>
        /// For example: "You have {0:apple;apples} left"
        /// </summary>
        /// <example>
        /// Usage:
        /// <code language="cs">
        /// String.Format(new PluralFormatProvider(), "You have {0:life;lives} left, {1:apple;apples} and {2:eye;eyes}.", 1, 0, 2);
        /// </code>
        /// </example>
        public static string FormatPlural(this string resourceValueWithPluralPlaceholders, params object[] values)
        {
            return String.Format(new PluralFormatProvider(), resourceValueWithPluralPlaceholders, values);
        }
    }
}
