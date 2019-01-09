using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class ArcRingSpinner : SpinnerBase
    {
        static ArcRingSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ArcRingSpinner),
                new FrameworkPropertyMetadata(typeof(ArcRingSpinner)));
        }
    }
}