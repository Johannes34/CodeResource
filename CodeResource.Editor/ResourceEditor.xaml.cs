using CodeResource;
using Microsoft.Win32;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace CodeResource.Editor
{
    /// <summary>
    /// Interaction logic for ResourceEditor.xaml
    /// </summary>
    public partial class ResourceEditor : UserControl, INotifyPropertyChanged
    {
        public ResourceEditor()
        {
            InitializeComponent();

            var defaultEntry = new ResourceEntry() { ResourceName = "NewEntry", Type = ResourceType.String };
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "de", Value = "Neuer Wert" });
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "en", Value = "New value" });

            Manager = new ResourceManager(defaultEntry);

            grid.AddingNewItem += Grid_AddingNewItem;
            grid.RowEditEnding += Grid_RowEditEnding;
            grid.CellEditEnding += Grid_CellEditEnding;
        }

        public ResourceManager Manager
        {
            get { return (ResourceManager)GetValue(ManagerProperty); }
            set { SetValue(ManagerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Manager.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ManagerProperty =
            DependencyProperty.Register("Manager", typeof(ResourceManager), typeof(ResourceEditor), new PropertyMetadata(null, (s, e) => 
            {
                var editor = s as ResourceEditor;
                editor?.CreateColumns();

                if (editor?.Manager != null)
                {
                    editor.FilteredResourceEntries = CollectionViewSource.GetDefaultView(editor.Manager.Resources);
                    editor.FilteredResourceEntries.Filter = editor.ApplyFilter;
                    editor.PropertyChanged?.Invoke(editor, new PropertyChangedEventArgs(nameof(FilteredResourceEntries)));

                    foreach (var resource in editor.Manager.Resources)
                        editor.ValidateEntry(resource);
                    editor.ValidateDuplicates();
                }
            }));

        public ICollectionView FilteredResourceEntries { get; private set; }

        private void CreateColumns()
        {
            grid.Columns.Clear();
            grid.RowValidationRules.Clear();

            if (Manager == null)
                return;

            var multiLineTextBlockStyle = new Style(typeof(TextBlock));
            multiLineTextBlockStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));

            var multiLineTextBoxStyle = new Style(typeof(TextBox));
            multiLineTextBoxStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.Wrap));
            multiLineTextBoxStyle.Setters.Add(new Setter(TextBox.AcceptsReturnProperty, true));

            var dataGridCopyButtonColumnTemplate = FindResource("dataGridCopyButtonColumnTemplate") as DataTemplate;

            grid.Columns.Add(new DataGridTextColumn() { 
                Header = "Resource Name", 
                Binding = new Binding(nameof(ResourceEntry.ResourceName)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                ElementStyle = multiLineTextBlockStyle,
                EditingElementStyle = multiLineTextBoxStyle,
                Width = 250 });

            foreach (var key in Manager.DefinedKeys)
            {
                grid.Columns.Add(new DataGridTextColumn() 
                { 
                    Header = key, 
                    Binding = new Binding($"{nameof(ResourceEntry.ResourceValues)}[{key}].{nameof(ResourceKeyValue.Value)}") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                    ElementStyle = multiLineTextBlockStyle,
                    EditingElementStyle = multiLineTextBoxStyle,
                    Width = 250
                });
            }

            grid.Columns.Add(new DataGridComboBoxColumn() { 
                Header = "Type",
                ItemsSource = Enum.GetValues<ResourceType>(), 
                SelectedItemBinding = new Binding(nameof(ResourceEntry.Type)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged } });

            grid.Columns.Add(new DataGridTextColumn() { 
                Header = "Comment", 
                Binding = new Binding(nameof(ResourceEntry.Comment)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                ElementStyle = multiLineTextBlockStyle,
                EditingElementStyle = multiLineTextBoxStyle,
                Width = 200 });

            grid.Columns.Add(new DataGridTemplateColumn() { 
                Header = "Copy", 
                CanUserSort = false, 
                CanUserResize = false, 
                IsReadOnly = true, 
                CellTemplate = dataGridCopyButtonColumnTemplate });

            grid.RowValidationRules.Add(new ResourceEntryValidationRule(this) { ValidationStep = ValidationStep.UpdatedValue });
            //grid.RowValidationRules.Add(new ResourceEntryValidationRule(Manager) { ValidationStep = ValidationStep.UpdatedValue });
        }

        public Action OnResourcesChanged { get; set; }

        // TODO: add, delete, rename languages

        // TODO: paste multiple values into grid

        #region Settings

        private string m_OpenAiAPIKey;
        public string OpenAiAPIKey
        {
            get { return m_OpenAiAPIKey; }
            set
            {
                if (value != m_OpenAiAPIKey)
                {
                    m_OpenAiAPIKey = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenAiAPIKey)));
                }
            }
        }

        private bool m_EnablePlaceholderNumberPatterns;
        public bool EnablePlaceholderNumberPatterns
        {
            get { return m_EnablePlaceholderNumberPatterns; }
            set
            {
                if (value != m_EnablePlaceholderNumberPatterns)
                {
                    m_EnablePlaceholderNumberPatterns = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnablePlaceholderNumberPatterns)));
                }
            }
        }

        private bool m_EnablePluralizedPlaceholderPatterns;
        public bool EnablePluralizedPlaceholderPatterns
        {
            get { return m_EnablePluralizedPlaceholderPatterns; }
            set
            {
                if (value != m_EnablePluralizedPlaceholderPatterns)
                {
                    m_EnablePluralizedPlaceholderPatterns = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnablePluralizedPlaceholderPatterns)));
                }
            }
        }


        #endregion

        #region Validation

        public class ResourceEntryValidationRule : ValidationRule
        {
            public ResourceEntryValidationRule(ResourceEditor editor)
            {
                m_editor = editor;
            }
            private ResourceEditor m_editor;

            public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
            {
                var resource = (value as BindingGroup)?.Items[0] as ResourceEntry;

                m_editor.ValidateEntry(resource);
                return ValidationResult.ValidResult;
            }
        }

        private static Regex variableName = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static Regex placeholderSuffix = new Regex("_[0-9]+S$", RegexOptions.Compiled);

        internal void ValidateDuplicates()
        {
            // no duplicate names:
            var byName = Manager.Resources.GroupBy(r => r.ResourceName);
            foreach (var name in byName.Where(n => n.Count() > 1))
            {
                var msg = "There {0:is;are} {0} resource {0:entry;entries} with the same name.".FormatPlural(name.Count() - 1);
                foreach (var resource in name)
                    resource.SetValidation(false, msg);
            }
        }

        internal bool ValidateEntry(ResourceEntry resource)
        {
            // 1. check if resource name is valid:
            // empty:
            if (String.IsNullOrEmpty(resource.ResourceName))
                return resource.SetValidation(false, "The Resource Name must be set.");
            // illegal chars, illegal format e.g. starting with digit:
            if (!variableName.IsMatch(resource.ResourceName))
                return resource.SetValidation(false, "The Resource Name has an illegal format. It may only contain letters, digits and underscores and must not start with a digit.");
            // missing suffix (_1S) for placeholders:
            int placeholders = resource.ResourceValues.First().PlaceholderCount;
            var match = placeholderSuffix.Match(resource.ResourceName);
            if (match.Success)
            {
                if (placeholders == 0)
                    return resource.SetValidation(false, "The Resource Name ends with a placeholder suffix, but there are no placeholders in the value.");
                else if (placeholders.ToString() != match.Value.Substring(1, match.Value.Length - 2))
                    return resource.SetValidation(false, $"The Resource Name ends with {match.Value}, but is expected to end with _{placeholders}S.");
            }
            else if (placeholders > 0)
            {
                return resource.SetValidation(false, $"The Resource Name is expected to end with the suffix '_{placeholders}S' indicating the number of placeholders.");
            }



            // 2. check if resource values are valid:
            // missing default localization:
            var defaultKey = Manager.DefaultKey ?? Manager.DefinedKeys.FirstOrDefault();
            var defaultValue = resource.ResourceValues.FirstOrDefault(v => v.Key == defaultKey)?.Value;
            if (String.IsNullOrEmpty(defaultValue))
                return resource.SetValidation(false, $"The value for the default localization '{defaultKey}' is missing.");
            // different placeholder counts:
            var placeholderCounts = resource.ResourceValues.GroupBy(v => v.PlaceholderCount);
            if (placeholderCounts.Count() > 1)
            {
                var err = String.Join(", ", placeholderCounts.Select(p => "{0} {1:has;have} {2} {2:placeholder;placeholders}".FormatPlural(String.Join(", ", p.Select(rkv => rkv.Key)), p.Count(), p.Key)));
                return resource.SetValidation(false, $"Not all values have the same amount of placeholders. " + err);
            }
            // different placeholder identifiers:
            // TODO..
            // resource values with illegal format (according to resource type):
            // TODO..

            return resource.SetValidation(true);
        }

        #endregion

        #region Filter

        private string m_FilterString;
        public string FilterString
        {
            get
            {
                return m_FilterString;
            }
            set
            {
                if (m_FilterString != value)
                {
                    m_FilterString = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilterString)));
                    FilteredResourceEntries?.Refresh();
                }
            }
        }

        private bool m_ShowEntriesWithMissingLocalization = false;
        public bool ShowEntriesWithMissingLocalization
        {
            get
            {
                return m_ShowEntriesWithMissingLocalization;
            }
            set
            {
                if (m_ShowEntriesWithMissingLocalization != value)
                {
                    m_ShowEntriesWithMissingLocalization = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEntriesWithMissingLocalization)));
                    FilteredResourceEntries?.Refresh();
                }
            }
        }

        private bool m_ShowEntriesWithErrors = false;
        public bool ShowEntriesWithErrors
        {
            get { return m_ShowEntriesWithErrors; }
            set
            {
                if (value != m_ShowEntriesWithErrors)
                {
                    m_ShowEntriesWithErrors = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEntriesWithErrors)));
                    FilteredResourceEntries?.Refresh();
                }
            }
        }


        private bool ApplyFilter(object item)
        {
            var entry = item as ResourceEntry;
            if (entry != null)
            {
                if (ShowEntriesWithErrors && !entry.IsErroneous)
                    return false;

                if (ShowEntriesWithMissingLocalization && !entry.HasMissingValues)
                    return false;

                if (!String.IsNullOrEmpty(FilterString))
                {
                    bool containsFilterString = 
                        (entry.ResourceName?.Contains(FilterString, StringComparison.OrdinalIgnoreCase) ?? false)
                        || entry.Type.ToString().Contains(FilterString, StringComparison.OrdinalIgnoreCase)
                        || (entry.Comment?.Contains(FilterString, StringComparison.OrdinalIgnoreCase) ?? false)
                        || entry.ResourceValues.Any(v => v.Value?.Contains(FilterString, StringComparison.OrdinalIgnoreCase) ?? false);
                    if (!containsFilterString)
                        return false;
                }
            }
            return true;
        }

        #endregion

        #region Grid editing events

        private void Grid_AddingNewItem(object? sender, AddingNewItemEventArgs e)
        {
            var resource = new ResourceEntry();
            foreach (var key in Manager.DefinedKeys)
                resource.ResourceValues.Add(new ResourceKeyValue() { Key = key, Value = null });
            e.NewItem = resource;
        }

        private void Grid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Cancel)
                return;
            
            var editedResource = e.Row.Item as ResourceEntry;

            // generate resource name from first filled localization:
            if (String.IsNullOrEmpty(editedResource?.ResourceName))
            {
                if (Manager.DefinedKeys.Contains(e.Column.Header as string))
                {
                    if (e.EditingElement is TextBox tb && !String.IsNullOrEmpty(tb.Text))
                    {
                        editedResource.ResourceName = Manager.GetResourceNameFromValue(tb.Text, out var initialPlaceholders, out string modifiedResourceValue);
                        if (modifiedResourceValue != tb.Text)
                            tb.Text = modifiedResourceValue;
                        editedResource.PastedPlaceholderValues = initialPlaceholders;
                    }
                }
            }

            ValidateEntry(editedResource);
            ValidateDuplicates();

            OnResourcesChanged?.Invoke();
        }

        private void Grid_RowEditEnding(object? sender, DataGridRowEditEndingEventArgs e)
        {
            if (grid.SelectedItem == null)
                return;

            grid.RowEditEnding -= Grid_RowEditEnding;
            grid.CommitEdit();
            grid.Items.Refresh();
            grid.RowEditEnding += Grid_RowEditEnding;

            // check if the newly added/edited row has a duplicated/illegal key:
            var editedResource = e.Row.Item as ResourceEntry;

        }

        #endregion

        #region Start cell edit on first click

        private object m_mouseDownItem;

        private void grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && m_mouseDownItem != null)
            {
                var releasedControl = grid.InputHitTest(e.GetPosition(grid));
                if (releasedControl == m_mouseDownItem && grid.SelectedCells.Count == 1)
                {
                    grid.BeginEdit();
                }
            }
        }

        private void grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedControl = grid.InputHitTest(e.GetPosition(grid));
            m_mouseDownItem = clickedControl;
        }


        #endregion

        #region Only accept shift+Enter for newline in textboxes, commit cell edit with pure Enter

        private void grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                // for whatever reason, two commits are required to apply the new value to the DataContext properties:
                grid.CommitEdit();
                grid.CommitEdit();
            }
        }

        #endregion

        #region OpenAI - Auto Localization

        private async void AutoLocalize_Click(object sender, RoutedEventArgs e)
        {
            var tb = new TextBlock() { MinWidth = 200, MinHeight = 100, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            tb.Text = "Please wait, localizing all missing values...";
            var popup = ShowPopupControl(tb, sender as Button, PlacementMode.Left);

            try
            {
                ChatClient client = new("gpt-3.5-turbo", OpenAiAPIKey);

                var groupedByMissingLanguage = new Dictionary<string, List<ResourceEntry>>();
                foreach (var resource in Manager.Resources)
                {
                    if (!resource.HasMissingValues)
                        continue;

                    if (resource.ResourceValues.All(v => String.IsNullOrEmpty(v.Value)))
                        continue;

                    if (String.IsNullOrEmpty(resource.ResourceValues.FirstOrDefault(v => v.Key == Manager.DefaultKey)?.Value))
                        continue;

                    foreach (var value in resource.ResourceValues.Where(v => String.IsNullOrEmpty(v.Value)))
                    {
                        if (!groupedByMissingLanguage.ContainsKey(value.Key))
                            groupedByMissingLanguage.Add(value.Key, new List<ResourceEntry>());
                        groupedByMissingLanguage[value.Key].Add(resource);
                    }
                }

                foreach (var missingLanguage in groupedByMissingLanguage)
                {
                    // build prompt:
                    var prompt = $"Translate the following items from {new CultureInfo(Manager.DefaultKey).EnglishName} to {new CultureInfo(missingLanguage.Key).EnglishName}:" + Environment.NewLine;

                    int i = 1;
                    foreach (var resourceEntry in missingLanguage.Value)
                    {
                        prompt += i + ". " + resourceEntry.ResourceValues.FirstOrDefault(v => v.Key == Manager.DefaultKey)?.Value + Environment.NewLine;
                        i++;
                    }

                    // send request:
                    ChatCompletion result = await client.CompleteChatAsync(new UserChatMessage(prompt));

                    // process response:
                    int j = 1;
                    string localizedValue = "";
                    var lines = result.ToString().Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith((j + 1) + ". "))
                        {
                            // commit current localization:
                            missingLanguage.Value[j - 1].ResourceValues[missingLanguage.Key].Value = localizedValue;
                            // increment counter:
                            j++;
                        }

                        if (line.StartsWith(j + ". "))
                        {
                            localizedValue = line.Substring(j.ToString().Length + 2);
                        }
                        else
                        {
                            localizedValue += Environment.NewLine + line;
                        }
                    }

                    // commit last localization:
                    missingLanguage.Value[j - 1].ResourceValues[missingLanguage.Key].Value = localizedValue;

                }

                popup.IsOpen = false;
            }
            catch (Exception ex)
            {
                tb.Text = ex.Message;
            }
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        private static Popup ShowPopupControl(UIElement control, UIElement? popupPlacement, PlacementMode placement)
        {
            var border = new Border();
            border.Background = Brushes.White;
            border.BorderBrush = Brushes.LightGray;
            border.BorderThickness = new Thickness(1);
            border.Child = control;

            var popup = new Popup();
            popup.Child = border;
            popup.StaysOpen = false;
            popup.PlacementTarget = popupPlacement;
            popup.Placement = placement;
            popup.PopupAnimation = PopupAnimation.Fade;
            popup.AllowsTransparency = true;
            popup.IsOpen = true;

            return popup;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entry = button?.DataContext as ResourceEntry;

            if (entry != null)
            {
                var copyView = new CopyResourceView(Manager, entry);
                copyView.Margin = new Thickness(3);

                ShowPopupControl(copyView, button, PlacementMode.Right);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            var settingsView = new Settings(Manager);
            settingsView.Margin = new Thickness(3);

            ShowPopupControl(settingsView, button, PlacementMode.Left);
        }

        private void CreateFromXaml_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            var importView = new ImportFromXaml(this);
            importView.Margin = new Thickness(3);

            ShowPopupControl(importView, button, PlacementMode.Left);
        }

        private void Languages_Click(object sender, RoutedEventArgs e)
        {
            // TODO: show popup dialog
            // - list of all languages
            // - each with checkbox
            // - filter text box
        }
    }

}
