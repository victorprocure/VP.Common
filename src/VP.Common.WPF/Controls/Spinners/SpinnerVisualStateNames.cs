// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IndicatorVisualStateNames.cs" company="Procure Software Development">
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
    internal class SpinnerVisualStateNames : MarkupExtension
    {
        private static SpinnerVisualStateNames _activeState;
        private static SpinnerVisualStateNames _inactiveState;

        public static SpinnerVisualStateNames ActiveState =>
            _activeState ?? (_activeState = new SpinnerVisualStateNames("Active"));

        public static SpinnerVisualStateNames InactiveState =>
            _inactiveState ?? (_inactiveState = new SpinnerVisualStateNames("Inactive"));

        private SpinnerVisualStateNames(string name)
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