using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RadioDataParser
{
    /// <summary>
    /// Utility fuctions for parsing FCC ULS search data.
    /// </summary>
    public static class Util
    {
        /// <summary>
        /// Get a description from an enum.
        /// </summary>
        /// <param name="value">Enum to convert.</param>
        /// <returns>Description of enum.</returns>
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        /// <summary>
        /// Convert a name to a proper case.
        /// </summary>
        /// <param name="s">Name to convert.</param>
        /// <returns>Name converted to proper case.</returns>
        public static string ToTitleCase(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                //Special case for numbered suffixes
                if(s.Equals("II") || s.Equals("III"))
                {
                    return s;
                }

                return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
            }

            return null;
        }
    }
}
