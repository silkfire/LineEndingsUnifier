namespace LineEndingsUnifier
{
    using System.Windows;
    using System.Windows.Controls;

    internal partial class LineEndingChoice
    {
        public LineEndingChoice(string windowTitle, LineEndingsChanger.LineEnding defaultLineEnding)
        {
            InitializeComponent();
            Title = windowTitle;

            switch (defaultLineEnding)
            {
                case LineEndingsChanger.LineEnding.Windows:
                    Windows_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEnding.Linux:
                    Linux_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEnding.Macintosh:
                    Macintosh_RadioButton.IsChecked = true;
                    break;
                case LineEndingsChanger.LineEnding.Dominant:
                    Dominant_RadioButton.IsChecked = true;
                    break;
            }
        }

        public LineEndingsChanger.LineEnding LineEnding { get; private set; } = LineEndingsChanger.LineEnding.None;

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var tag = (sender as RadioButton)?.Tag?.ToString();

            LineEnding = System.Enum.TryParse(tag, out LineEndingsChanger.LineEnding lineEnding)
                ? lineEnding
                : LineEndingsChanger.LineEnding.None;
        }

        private void Change_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            LineEnding = LineEndingsChanger.LineEnding.None;
            DialogResult = false;
            Close();
        }
    }
}
