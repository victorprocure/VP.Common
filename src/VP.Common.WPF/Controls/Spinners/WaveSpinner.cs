using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class WaveSpinner : SpinnerBase
    {
        static WaveSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveSpinner),
                new FrameworkPropertyMetadata(typeof(WaveSpinner)));
        }
    }
}