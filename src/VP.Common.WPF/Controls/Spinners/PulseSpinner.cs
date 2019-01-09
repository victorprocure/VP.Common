using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class PulseSpinner : SpinnerBase
    {
        static PulseSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PulseSpinner),
                new FrameworkPropertyMetadata(typeof(PulseSpinner)));
        }
    }
}