using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class ThreeDotSpinner : SpinnerBase
    {
        static ThreeDotSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThreeDotSpinner),
                new FrameworkPropertyMetadata(typeof(ThreeDotSpinner)));
        }
    }
}