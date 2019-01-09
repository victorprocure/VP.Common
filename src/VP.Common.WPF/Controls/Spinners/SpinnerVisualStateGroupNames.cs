// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IndicatorVisualStateGroupNames.cs" company="Procure Software Development">
//     Copyright (c) 2019 Procure Software Development
// </copyright>
// <author></author>
// <summary>
//     
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows.Markup;

namespace VP.Common.WPF.Controls.Spinners
{
    internal class SpinnerVisualStateGroupNames : MarkupExtension
    {
        private static SpinnerVisualStateGroupNames _internalActiveStates;
        private static SpinnerVisualStateGroupNames _sizeStates;

        public static SpinnerVisualStateGroupNames ActiveStates =>
            _internalActiveStates ?? (_internalActiveStates = new SpinnerVisualStateGroupNames("ActiveStates"));

        public static SpinnerVisualStateGroupNames SizeStates =>
            _sizeStates ?? (_sizeStates = new SpinnerVisualStateGroupNames("SizeStates"));

        private SpinnerVisualStateGroupNames(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public string Name { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Name;
        }
    }
}