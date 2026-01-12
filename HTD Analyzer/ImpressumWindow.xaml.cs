using System.Windows;

namespace HiddenTextDetector
{
    public partial class ImpressumWindow : Window
    {
        public ImpressumWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}