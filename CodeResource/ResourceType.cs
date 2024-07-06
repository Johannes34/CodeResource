using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeResource
{
    public enum ResourceType
    {
        /// <summary>
        /// String value
        /// </summary>
        String,
        /// <summary>
        /// Margin or Padding, either with four values (left, top, right, bottom) e.g. 1,2,1,5 or one single value (uniform) format e.g. 5
        /// </summary>
        Thickness,
        /// <summary>
        /// Visibility, allowed values: Collapsed, Visible, Hidden
        /// </summary>
        Visibility,
        /// <summary>
        /// Number with digit separator, format e.g. 20000.58 or 23 or -2.3
        /// </summary>
        Double,
        /// <summary>
        /// SolidColorBrush, format see <see cref="Color"/>.
        /// </summary>
        Brush,
        /// <summary>
        /// Color, format either known Colors property (e.g. Red or White) or an ARGB value, e.g. 128, 30, 80, 120
        /// </summary>
        Color,
        /// <summary>
        /// Bool, allowed values: True, False
        /// </summary>
        Boolean
    }
}
