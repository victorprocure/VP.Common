using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class DoubleBounceSpinner : SpinnerBase
    {
        static DoubleBounceSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DoubleBounceSpinner),
                new FrameworkPropertyMetadata(typeof(DoubleBounceSpinner)));
        }
    }
}