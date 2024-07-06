using CodeResource;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
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
using System.Xml;
using System.Xml.Linq;

namespace CodeResource.Editor
{
    /// <summary>
    /// Interaction logic for ImportFromXaml.xaml
    /// </summary>
    public partial class ImportFromXaml : UserControl, INotifyPropertyChanged
    {
        public ImportFromXaml(ResourceEditor editor)
        {
            Editor = editor;
            Manager = editor.Manager;
            InitializeComponent();
        }

        private ResourceEditor m_Editor;
        public ResourceEditor Editor
        {
            get { return m_Editor; }
            set
            {
                if (value != m_Editor)
                {
                    m_Editor = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Editor)));
                }
            }
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

        private string m_XamlFilePath;
        public string XamlFilePath
        {
            get { return m_XamlFilePath; }
            set
            {
                if (value != m_XamlFilePath)
                {
                    m_XamlFilePath = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlFilePath)));
                }
            }
        }

        private string m_XamlAttributes = "*.Content, *.ToolTip, *.Text, *.Header";
        public string XamlAttributes
        {
            get { return m_XamlAttributes; }
            set
            {
                if (value != m_XamlAttributes)
                {
                    m_XamlAttributes = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(XamlAttributes)));
                }
            }
        }

        private string m_ImportLanguage = "en";
        public string ImportLanguage
        {
            get { return m_ImportLanguage; }
            set
            {
                if (value != m_ImportLanguage)
                {
                    m_ImportLanguage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ImportLanguage)));
                }
            }
        }

        private bool m_InsertBindings = true;
        public bool InsertBindings
        {
            get { return m_InsertBindings; }
            set
            {
                if (value != m_InsertBindings)
                {
                    m_InsertBindings = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(InsertBindings)));
                }
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            // TODO: button to browse for file or folder
            // TODO: allow folder path to process all xamls within the folder structure
            // TODO: also allow selecting .cs files

            var xaml = XElement.Load(XamlFilePath, LoadOptions.PreserveWhitespace);
            var fileName = System.IO.Path.GetFileName(XamlFilePath);

            // TODO: remember last input in UI elements?

            Dictionary<string, List<string>> elementAttributes = new Dictionary<string, List<string>>();
            foreach (var entry in XamlAttributes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var elementAttributePair = entry.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (elementAttributePair.Length != 2)
                    continue;

                if (!elementAttributes.ContainsKey(elementAttributePair[0]))
                    elementAttributes.Add(elementAttributePair[0], new List<string>());
                if (!elementAttributes[elementAttributePair[0]].Contains(elementAttributePair[1]))
                    elementAttributes[elementAttributePair[0]].Add(elementAttributePair[1]);
            }

            // TODO: split UI into two steps:
            // 1. first step will analyze the xaml file and read out all relevant values
            // 2. second step shows a checkbox list of all values and the option to insert bindings and to actually 'do'

            // read out all attributes:
            bool hasImportedResources = false;
            var allXamlNodes = xaml.DescendantNodesAndSelf().OfType<XElement>();
            foreach (var elementEntry in elementAttributes)
            {
                // collect all xml elements in xaml with matching name or all elements for wildcard patterns, e.g. Button.Content, *.ToolTip etc.
                var allElements = allXamlNodes.Where(a => elementEntry.Key == "*" || a.Name.LocalName == elementEntry.Key).ToArray();

                foreach (var element in allElements)
                {
                    foreach (var attributeName in elementEntry.Value)
                    {
                        var attribute = element.Attribute(attributeName);
                        // skip import if provided attribute does not exist on the element:
                        if (attribute == null)
                            continue;

                        // skip import of attributes with bindings or static resources etc.:
                        if (attribute.Value.StartsWith("{"))
                            continue;

                        // skip import of empty attribute values:
                        if (String.IsNullOrWhiteSpace(attribute.Value))
                            continue;

                        // create resource:
                        hasImportedResources = true;
                        var resourceName = Manager.GetResourceNameFromValue(attribute.Value, out var initialPlaceholders, out string modifiedResourceValue);

                        if (String.IsNullOrEmpty(resourceName))
                            continue;

                        // common UI string format patterns: ignore for resource entry, rather add it in UI binding via stringformat:
                        bool endsWithColon = false;
                        if (modifiedResourceValue.EndsWith(':'))
                        {
                            endsWithColon = true;
                            modifiedResourceValue = modifiedResourceValue.Substring(0, modifiedResourceValue.Length - 1);
                        }

                        // if resource name already exists, it should not be created but rather reused
                        var entry = Manager.Resources.FirstOrDefault(r => r.ResourceName == resourceName);
                        if (entry == null)
                        {
                            entry = new ResourceEntry() { ResourceName = resourceName, Comment = $"Imported from {fileName}" };
                            foreach (var key in Manager.DefinedKeys)
                                entry.ResourceValues.Add(new ResourceKeyValue() { Key = key, Value = (key == ImportLanguage ? modifiedResourceValue : null) });
                            Manager.Resources.Add(entry);
                        }
                        else
                        {
                            // resource exists, but doesnt have a value for the language set -> set value:
                            var existingValue = entry.ResourceValues.FirstOrDefault(v => v.Key == ImportLanguage);
                            if (String.IsNullOrEmpty(existingValue?.Value))
                                existingValue.Value = modifiedResourceValue;
                            // resource exists, but has a different value for the language set as the one that would have been extracted -> create new resource entry:
                            else if (existingValue.Value != modifiedResourceValue)
                            {
                                // find free name for the resource:
                                int i = 1;
                                string newName = "";
                                while (entry != null)
                                {
                                    newName = $"{resourceName}_{i++}";
                                    entry = Manager.Resources.FirstOrDefault(r => r.ResourceName == newName);
                                }
                                resourceName = newName;

                                entry = new ResourceEntry() { ResourceName = resourceName, Comment = $"Imported from {fileName}" };
                                foreach (var key in Manager.DefinedKeys)
                                    entry.ResourceValues.Add(new ResourceKeyValue() { Key = key, Value = (key == ImportLanguage ? modifiedResourceValue : null) });
                                Manager.Resources.Add(entry);
                            }
                        }

                        // replace xaml attribute:
                        if (InsertBindings)
                        {
                            var binding = $"{{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={resourceName}}}";
                            if (endsWithColon)
                                binding = $"{{Binding Source={{x:Static res:{Manager.ClassName}.Instance}}, Path={resourceName}, StringFormat={{}}{{0}}:}}";
                            attribute.Value = binding;
                        }

                        Editor.ValidateEntry(entry);
                    }
                }
            }

            if (!hasImportedResources)
                return;

            if (InsertBindings)
            {
                // move original file to the recycle bin to avoid unwanted data loss:
                FileSystem.DeleteFile(XamlFilePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                // insert using:
                if (!xaml.Attributes().Any(a => a.Name.LocalName == "res"))
                    // TODO: auto add assembly info, based on res.cs parenting .csproj (file system wise) and xaml parenting csproj -> same csproj no add, different csproj add res.cs csproj name
                    xaml.SetAttributeValue(XName.Get("res", xaml.GetNamespaceOfPrefix("xmlns").ToString()), $"clr-namespace:{Manager.Namespace}");

                // save updated xaml file:
                // TODO: manually somehow restore the line breaks between xml attributes, see https://stackoverflow.com/questions/24413146/preserving-whitespace-within-xml-elements-between-attributes-when-using-xslcompi
                using (var writer = XmlWriter.Create(XamlFilePath, new XmlWriterSettings { OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.None }))
                    xaml.Save(writer);
            }

            Editor.ValidateDuplicates();

            Editor.OnResourcesChanged?.Invoke();
        }
    }

}
