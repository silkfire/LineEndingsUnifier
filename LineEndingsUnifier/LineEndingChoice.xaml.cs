namespace LineEndingsUnifier
{
    using System.Windows;
    using System.Windows.Controls;

    public partial class LineEndingChoice
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
            var buttonContent = (sender as RadioButton).Content.ToString();

            if (buttonContent.StartsWith("Windows"))
            {
                LineEnding = LineEndingsChanger.LineEnding.Windows;
            }
            else if (buttonContent.StartsWith("Linux"))
            {
                LineEnding = LineEndingsChanger.LineEnding.Linux;
            }
            else if (buttonContent.StartsWith("Macintosh"))
            {
                LineEnding = LineEndingsChanger.LineEnding.Macintosh;
            }
            else if (buttonContent.StartsWith("Dominant"))
            {
                LineEnding = LineEndingsChanger.LineEnding.Dominant;
            }
            else
            {
                LineEnding = LineEndingsChanger.LineEnding.None;
            }
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
