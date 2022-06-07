namespace LineEndingsUnifier
{
    using System.Windows;
    using System.Windows.Controls;

    public partial class LineEndingChoice : Window
    {
        public LineEndingChoice()
        {
            InitializeComponent();
        }

        public LineEndingChoice(string fileName, LineEndingsChanger.LineEndings defaultLineEnding)
        {
            InitializeComponent();
            this.Title = fileName;

            switch (defaultLineEnding)
            {
                case LineEndingsChanger.LineEndings.Dominant:
                    this.Dominant_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEndings.Linux:
                    this.Linux_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEndings.Macintosh:
                    this.Macintosh_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEndings.Windows:
                    this.Windows_RadioButton.IsChecked = true;
                    break;
                default:
                    break;
            }
        }

        public LineEndingsChanger.LineEndings LineEndings { get; private set; } = LineEndingsChanger.LineEndings.None;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;

            if (button.Content.ToString().Contains("Windows"))
            {
                this.LineEndings = LineEndingsChanger.LineEndings.Windows;
            }
            else if (button.Content.ToString().Contains("Linux"))
            {
                this.LineEndings = LineEndingsChanger.LineEndings.Linux;
            }
            else if (button.Content.ToString().Contains("Macintosh"))
            {
                this.LineEndings = LineEndingsChanger.LineEndings.Macintosh;
            }
            else if (button.Content.ToString().Contains("Dominant"))
            {
                this.LineEndings = LineEndingsChanger.LineEndings.Dominant;
            }
            else
            {
                this.LineEndings = LineEndingsChanger.LineEndings.None;
            }
        }

        private void Change_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.LineEndings = LineEndingsChanger.LineEndings.None;
            this.DialogResult = false;
            this.Close();
        }
    }
}
