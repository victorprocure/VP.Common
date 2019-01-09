using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class FlipPlaneSpinner : SpinnerBase
    {
        static FlipPlaneSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlipPlaneSpinner),
                new FrameworkPropertyMetadata(typeof(FlipPlaneSpinner)));
        }
    }
}