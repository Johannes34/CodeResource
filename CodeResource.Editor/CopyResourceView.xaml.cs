using CodeResource;
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

namespace CodeResource.Editor
{
    /// <summary>
    /// Interaction logic for CopyResourceView.xaml
    /// </summary>
    public partial class CopyResourceView : UserControl, INotifyPropertyChanged
    {
        public CopyResourceView(ResourceManager manager, ResourceEntry entry)
        {
            Manager = manager;
            Resource = entry;
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


        private ResourceEntry m_Resource;
        public ResourceEntry Resource
        {
            get
            {
                return m_Resource;
            }
            set
            {
                if (m_Resource != value)
                {
                    DataContext = value;
                    m_Resource = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Resource)));
                    if (value != null)
                    {
                        IsWithPluralizedPlaceholders = value.ResourceValues.Any(v => v.HasPluralization);
                        IsWithSimplePlaceholders = value.ResourceValues.Any(v => v.HasPlaceholders && !v.HasPluralization);
                        IsWithoutPlaceholder = value.ResourceValues.Any(v => !v.HasPlaceholders);
                        PlaceholderCount = value.ResourceValues.Select(v => v.PlaceholderCount).Max();
                        IsWithMultipleSimplePlaceholder = IsWithSimplePlaceholders && PlaceholderCount > 1;
                        IsWithOneSimplePlaceholder = IsWithSimplePlaceholders && !IsWithMultipleSimplePlaceholder;

                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(CodeUsing)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(CodeResourceName)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(CodeSimplePlaceholder)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(CodePluralizedPlaceholder)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(CodePluralizedPlaceholder2)));

                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlUsing)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlResourceNameBinding)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlResourceNameBindingWithFormatting)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlSimpleOneSlotPlaceholderBinding)));
                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlSimpleMultiSlotPlaceholderBinding)));

                        PropertyChanged(this, new PropertyChangedEventArgs(nameof(LocalizeRemainingAIPrompt)));
                    }
                }
            }
        }

        private bool m_IsWithoutPlaceholder;
        public bool IsWithoutPlaceholder
        {
            get
            {
                return m_IsWithoutPlaceholder;
            }
            set
            {
                if (m_IsWithoutPlaceholder != value)
                {
                    m_IsWithoutPlaceholder = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsWithoutPlaceholder)));
                }
            }
        }

        private bool m_IsWithSimplePlaceholders;
        public bool IsWithSimplePlaceholders
        {
            get
            {
                return m_IsWithSimplePlaceholders;
            }
            set
            {
                if (m_IsWithSimplePlaceholders != value)
                {
                    m_IsWithSimplePlaceholders = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsWithSimplePlaceholders)));
                }
            }
        }

        private int m_PlaceholderCount;
        public int PlaceholderCount
        {
            get
            {
                return m_PlaceholderCount;
            }
            set
            {
                if (m_PlaceholderCount != value)
                {
                    m_PlaceholderCount = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(PlaceholderCount)));
                }
            }
        }

        private bool m_IsWithOneSimplePlaceholder;
        public bool IsWithOneSimplePlaceholder
        {
            get
            {
                return m_IsWithOneSimplePlaceholder;
            }
            set
            {
                if (m_IsWithOneSimplePlaceholder != value)
                {
                    m_IsWithOneSimplePlaceholder = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsWithOneSimplePlaceholder)));
                }
            }
        }

        private bool m_IsWithMultipleSimplePlaceholder;
        public bool IsWithMultipleSimplePlaceholder
        {
            get
            {
                return m_IsWithMultipleSimplePlaceholder;
            }
            set
            {
                if (m_IsWithMultipleSimplePlaceholder != value)
                {
                    m_IsWithMultipleSimplePlaceholder = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsWithMultipleSimplePlaceholder)));
                }
            }
        }

        private bool m_IsWithPluralizedPlaceholders;
        public bool IsWithPluralizedPlaceholders
        {
            get
            {
                return m_IsWithPluralizedPlaceholders;
            }
            set
            {
                if (m_IsWithPluralizedPlaceholders != value)
                {
                    m_IsWithPluralizedPlaceholders = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsWithPluralizedPlaceholders)));
                }
            }
        }


        private string Placeholders(string defaultPlaceholder = "\"\"") => String.Join(", ", (Resource.PastedPlaceholderValues?.Any(v => v.Length > 1) ?? false) ? Resource.PastedPlaceholderValues : Enumerable.Repeat(defaultPlaceholder, PlaceholderCount));
        private List<string> SeparatePlaceholders(string defaultPlaceholder = "\"\"") => (Resource.PastedPlaceholderValues?.Any(v => v.Length > 1) ?? false) ? Resource.PastedPlaceholderValues : Enumerable.Repeat(defaultPlaceholder, PlaceholderCount).ToList();


        public string CodeUsing => $"using {Manager.Namespace};";
        public string CodeResourceName => $"{Manager.ClassName}.{Resource.ResourceName};";
        public string CodeSimplePlaceholder => $"String.Format({Manager.ClassName}.{Resource.ResourceName}, {Placeholders()});";
        public string CodePluralizedPlaceholder => $"String.Format(PluralFormatProvider.Default, {Manager.ClassName}.{Resource.ResourceName}, {Placeholders()});";
        public string CodePluralizedPlaceholder2 => $"{Manager.ClassName}.{Resource.ResourceName}.FormatPlural({Placeholders()});";


        public string XamlUsing => $"xmlns:res=\"clr-namespace:{Manager.Namespace}\"";
        public string XamlResourceNameBinding => $"{{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={Resource.ResourceName}}}";
        public string XamlResourceNameBindingWithFormatting => $"{{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={Resource.ResourceName}, StringFormat={{}}Oh, {{0}}!!}}";
        public string XamlSimpleOneSlotPlaceholderBinding => $"{{Binding Path={Placeholders("Name")}, StringFormat={{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={Resource.ResourceName}}}";
        public string XamlSimpleMultiSlotPlaceholderBinding => $"<MultiBinding StringFormat=\"{{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={Resource.ResourceName}}}\">{String.Join("", SeparatePlaceholders("Name").Select(p => $"\r\n  <Binding Path=\"{p}\" />"))}\r\n</MultiBinding>";


        public string LocalizeRemainingAIPrompt => $"Translate the following text to {String.Join(" and ", Resource.ResourceValues.Where(v => String.IsNullOrEmpty(v.Value)).Select((v, i) => $"{i+1}. {v.Key}"))}:\r\n{Resource.ResourceValues.FirstOrDefault(v => !String.IsNullOrEmpty(v.Value))?.Value}";


        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            var textbox = ((sender as Button)?.Parent as StackPanel)?.Children.OfType<FrameworkElement>().ElementAtOrDefault(1) as TextBox;
            if (textbox != null)
                Clipboard.SetText(textbox.Text);
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
                return ConvertBack(value, targetType, parameter, culture);
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool && targetType == typeof(Visibility))
                return Convert(value, targetType, parameter, culture);
            return (Visibility)value == Visibility.Visible ? true : false;
        }
    }

    public class ReferenceToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? true : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
