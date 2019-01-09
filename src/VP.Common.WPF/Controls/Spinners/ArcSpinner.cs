using System.Windows;

namespace VP.Common.WPF.Controls.Spinners
{
    public sealed class ArcSpinner : SpinnerBase
    {
        static ArcSpinner(){
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ArcSpinner),
                new FrameworkPropertyMetadata(typeof(ArcSpinner)));
        }
    }
}