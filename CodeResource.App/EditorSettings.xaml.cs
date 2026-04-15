using CodeResource;
using CodeResource.Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodeResource.Demo
{
    /// <summary>
    /// Interaction logic for EditorSettings.xaml
    /// </summary>
    public partial class EditorSettings : UserControl, INotifyPropertyChanged
    {
        public EditorSettings(ResourceManager manager)
        {
            Manager = manager;
            InitializeComponent();
        }

        private ResourceManager m_Manager;
        public ResourceManager Manager
        {
            get
            {
                return m_Manager;
            }
            set
            {
                if (m_Manager != value)
                {
                    m_Manager = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Manager)));
                }
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

    }

}
