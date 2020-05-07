using LINQPad.Extensibility.DataContext;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace Overby.LINQPad.FileDriver
{
    public partial class ConnectionDialog : Window
    {
        private readonly ConnectionProperties _model;

        public ConnectionDialog(IConnectionInfo cxInfo)
        {

            // ConnectionProperties is your view-model.
            DataContext = _model = new ConnectionProperties(cxInfo);

            InitializeComponent();
        }

        void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var d = new CommonOpenFileDialog { IsFolderPicker = true, EnsureFileExists = true };
            if (d.ShowDialog() == CommonFileDialogResult.Ok)
            {
                _model.DataDirectoryPath = d.FileName;
            }
        }
    }
}