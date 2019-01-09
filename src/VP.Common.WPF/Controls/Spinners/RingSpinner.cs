using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class RingSpinner : SpinnerBase
    {
        static RingSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RingSpinner),
                new FrameworkPropertyMetadata(typeof(RingSpinner)));
        }
    }
}