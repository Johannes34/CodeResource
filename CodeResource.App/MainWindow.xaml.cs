using CodeResource.Editor;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();

            New_Click(null, null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;


        private ResourceManager m_Manager;
        public ResourceManager Manager
        {
            get { return m_Manager; }
            set
            {
                if (value != m_Manager)
                {
                    m_Manager = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Manager)));
                }
            }
        }


        private void New_Click(object sender, RoutedEventArgs e)
        {
            var defaultEntry = new ResourceEntry() { ResourceName = "NewEntry", Type = ResourceType.String };
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "de", Value = "Neuer Wert" });
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "en", Value = "New value" });

            Manager = new ResourceManager(defaultEntry);
            Title = "Code Resource Editor - Unsaved";
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Code Resource file (*.res.cs)|*.res.cs|All files (*.*)|*.*";
            ofd.Multiselect = false;
            ofd.AddExtension = true;
            if (ofd.ShowDialog() == true)
            {
                var file = new FileInfo(ofd.FileName);
                var manager = new ResourceManager();
                manager.LoadResources(file);
                Manager = manager;
                Title = $"Code Resource Editor - {file.Name}";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.ResourceFile == null || !Manager.ResourceFile.Exists)
            {
                var sfd = new SaveFileDialog();
                sfd.Filter = "Code Resource file (*.res.cs)|*.res.cs";
                if (sfd.ShowDialog() != true)
                    return;

                var fi = new FileInfo(sfd.FileName);
                Manager.SaveResources(fi);
                Title = $"Code Resource Editor - {fi.Name}";
                return;
            }

            Manager.SaveResources();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }


        private void Language_Click(object sender, RoutedEventArgs e)
        {
            var clickedLanguage = e.OriginalSource as MenuItem;
            var clickedKeyName = clickedLanguage?.Name;

            if (clickedKeyName != null)
            {
                Resource.Instance.Key = clickedKeyName;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var editorSettings = new EditorSettings(Manager);
            editorSettings.Margin = new Thickness(3);
            editorSettings.MaxWidth = this.Width - 80;
            editorSettings.MaxHeight = this.Height - 80;


            var border = new Border();
            border.Background = Brushes.White;
            border.BorderBrush = Brushes.LightGray;
            border.BorderThickness = new Thickness(1);
            border.Child = editorSettings;

            var popup = new Popup();
            popup.Child = border;
            popup.StaysOpen = false;
            popup.PlacementTarget = root.Content as UIElement;
            popup.Placement = PlacementMode.Center;
            popup.PopupAnimation = PopupAnimation.Fade;
            popup.AllowsTransparency = true;
            popup.IsOpen = true;
            
            popup.Closed += (s, e) => overlay.Visibility = Visibility.Collapsed;
            overlay.Visibility = Visibility.Visible;

        }
    }
}