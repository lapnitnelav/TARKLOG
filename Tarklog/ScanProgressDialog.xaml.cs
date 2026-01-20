using System.Windows;

namespace Tarklog
{
    public partial class ScanProgressDialog : Window
    {
        public ScanProgressDialog()
        {
            InitializeComponent();
        }

        public void SetPhase(int phase, string description)
        {
            Dispatcher.Invoke(() =>
            {
                PhaseLabel.Text = $"Phase {phase}: {description}";
                ProgressBar.IsIndeterminate = true;
            });
        }

        public void UpdateProgress(int current, int total, string activity)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Maximum = total;
                ProgressBar.Value = current;
                ProgressText.Text = $"{current} / {total}";
                CurrentActivityLabel.Text = activity;
            });
        }

        public void UpdateStats(int filesFound, int itemsParsed)
        {
            Dispatcher.Invoke(() =>
            {
                FilesFoundLabel.Text = filesFound.ToString();
                ItemsParsedLabel.Text = itemsParsed.ToString();
            });
        }

        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                CurrentActivityLabel.Text = message;
            });
        }
    }
}
