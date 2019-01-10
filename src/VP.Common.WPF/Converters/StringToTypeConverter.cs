using System.ComponentModel;
using System.Globalization;
using VP.Common.Core.Reflection;

namespace VP.Common.WPF.Converters
{
    public class StringToTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, System.Type sourceType)
        {
            return sourceType.IsAssignableFrom(typeof(string));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var typeName = value as string;

            if(string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            return TypeCache.GetType(typeName, allowInitialization: false);
        }
    }
}