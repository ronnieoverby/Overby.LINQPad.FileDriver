using LINQPad.Extensibility.DataContext;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Overby.LINQPad.FileDriver
{
    /// <summary>
    /// Wrapper to read/write connection properties. This acts as our ViewModel - we will bind to it in ConnectionDialog.xaml.
    /// </summary>
    class ConnectionProperties : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IConnectionInfo ConnectionInfo { get; private set; }

        XElement DriverData => ConnectionInfo.DriverData;

        public ConnectionProperties(IConnectionInfo cxInfo)
        {
            ConnectionInfo = cxInfo;
        }

        public string DisplayName 
        {
            get => ConnectionInfo.DisplayName;
            set
            {
                if (value == ConnectionInfo.DisplayName)
                    return;

                ConnectionInfo.DisplayName = value;
                PropChanged();
            }
        }

        public string DataDirectoryPath
        {
            get => (string)GetElement();
            set
            {
                if (DataDirectoryPath?.Equals(value) == true)
                    return;

                var setDisplayName =
                    string.IsNullOrWhiteSpace(DisplayName)
                    || (TryGetDirectoryInfo(DataDirectoryPath, out var originalDirectory)
                            && originalDirectory.Name == DisplayName);

                SetDriverData(value);

                if (setDisplayName && TryGetDirectoryInfo(value, out var directoryInfo))
                    DisplayName = directoryInfo.Name;
            }
        }

        bool TryGetDirectoryInfo(string s, out DirectoryInfo directoryInfo)
        {
            try
            {
                directoryInfo = new DirectoryInfo(s);
                return true;
            }
            catch
            {
                directoryInfo = default;
                return false;
            }
        }

        XElement GetElement([CallerMemberName]string name = null) =>
            DriverData.Element(name);

        void SetDriverData<T>(T value, [CallerMemberName]string name = null, bool signalPropChange = true)
        {
            DriverData.SetElementValue(name, value);

            if (signalPropChange)
                PropChanged(name);
        }

        void PropChanged([CallerMemberName]string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}