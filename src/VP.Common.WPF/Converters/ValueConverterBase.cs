using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using VP.Common.Core.Reflection;

namespace VP.Common.WPF.Converters
{
    public abstract class ValueConverterBase : ValueConverterBase<object>
    {

    }

    public abstract class ValueConverterBase<TConversionInput> : ValueConverterBase<TConversionInput, object>
    {

    }

    public abstract class ValueConverterBase<TConversionInput, TConversionInputSource> : MarkupExtension, IValueConverter
    {
        public ValueConverterBase()
        {
            HasInvertCommandParameter = true;
        }

        protected CultureInfo CurrentCulture { get; private set; }

        public bool HasInvertCommandParameter { get; set; }

        public IValueConverter Link { get; set; }

        [TypeConverter(typeof(StringToTypeConverter))]
        public Type OverrideType { get; set; }

        [TypeConverter(typeof(StringToTypeConverter))]
        public Type SourceOverrideType { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CurrentCulture = culture;

            var returnValue = value;

            if (!(Link is null))
            {
                returnValue = Link.Convert(returnValue, OverrideType ?? targetType, parameter, culture);
            }

            if (!CanConvert<TConversionInput>(value))
            {
                return DependencyProperty.UnsetValue;
            }

            returnValue = Convert((TConversionInput)returnValue, targetType, parameter);

            return returnValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CurrentCulture = culture;

            var returnValue = value;

            if (!CanConvert<TConversionInputSource>(value))
            {
                returnValue = DependencyProperty.UnsetValue;
            }

            returnValue = ConvertBack((TConversionInputSource)returnValue, targetType, parameter);

            if (!(Link is null))
            {
                returnValue = Link.ConvertBack(returnValue, SourceOverrideType ?? targetType, parameter, culture);
            }

            return returnValue;
        }

        protected abstract object Convert(TConversionInput value, Type targetType, object parameter);

        protected virtual object ConvertBack(TConversionInputSource value, Type targetType, object parameter)
        {
            return DependencyProperty.UnsetValue;
        }

        protected virtual bool CanConvert<T>(object value)
        {
            if (!ReferenceEquals(value, null))
            {
                return value is T;
            }

            return typeof(T).IsNullable();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}