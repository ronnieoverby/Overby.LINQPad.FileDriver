using LINQPad.Extensibility.DataContext;
using System.ComponentModel;
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

        public string DataDirectoryPath
        {
            get => (string)GetElement();
            set
            {
                if (DataDirectoryPath?.Equals(value) == true)
                    return;

                SetDriverData(value);
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