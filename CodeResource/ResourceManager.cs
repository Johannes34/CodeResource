using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeResource
{
    public class ResourceManager : INotifyPropertyChanged
    {
        public ResourceManager()
        { }

        public ResourceManager(params ResourceEntry[] resources)
        {
            foreach (var resource in resources)
                Resources.Add(resource);
        }

        public ObservableCollection<ResourceEntry> Resources { get; } = new ObservableCollection<ResourceEntry>();

        public string[] DefinedKeys => Resources.SelectMany(r => r.ResourceValues).Select(r => r.Key).Distinct().ToArray();

        public bool HasMissingLocalizationValues => Resources.Any(r => r.HasMissingValues);


        private FileInfo m_ResourceFile;
        public FileInfo ResourceFile
        {
            get
            {
                return m_ResourceFile;
            }
            private set
            {
                if (m_ResourceFile != value)
                {
                    m_ResourceFile = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ResourceFile)));
                }
            }
        }

        private string m_DefaultKey;
        public string DefaultKey
        {
            get
            {
                return m_DefaultKey;
            }
            set
            {
                if (m_DefaultKey != value)
                {
                    m_DefaultKey = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(DefaultKey)));
                }
            }
        }

        private string m_Namespace = "ResourceGenerator";
        public string Namespace
        {
            get
            {
                return m_Namespace;
            }
            set
            {
                if (m_Namespace != value)
                {
                    m_Namespace = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Namespace)));
                }
            }
        }

        private string m_ClassName = "Resources";
        public string ClassName
        {
            get
            {
                return m_ClassName;
            }
            set
            {
                if (m_ClassName != value)
                {
                    m_ClassName = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ClassName)));
                }
            }
        }


        private string m_separatorLine = "// beginning of all resource keys:";

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void AddResourceKey(string key)
        {
            foreach (var resource in Resources)
            {
                if (!resource.ResourceValues.Any(v => v.Key == key))
                    resource.ResourceValues.Add(new ResourceKeyValue() { Key = key, Value = null });
            }
        }

        public void RemoveResourceKey(string key)
        {
            foreach (var resource in Resources)
            {
                var existing = resource.ResourceValues.FirstOrDefault(v => v.Key == key);
                if (existing != null)
                    resource.ResourceValues.Remove(existing);
            }
        }

        public void LoadResources(FileInfo file)
        {
            ResourceFile = file;

            var code = File.ReadAllText(file.FullName);

            LoadResources(code);
        }
        
        public void LoadResources(string code)
        {
            var keysStartIndex = code.IndexOf(m_separatorLine);
            var codeHeader = code.Substring(0, keysStartIndex);

            var namespaceMatch = Regex.Match(codeHeader, "namespace (?<match>\\S+?);");
            if (!namespaceMatch.Success)
                throw new Exception("The namespace could not be matched.");
            Namespace = namespaceMatch.Groups["match"].Value;


            var classMatch = Regex.Match(codeHeader, "public class (?<match>\\w+?) : INotifyPropertyChanged");
            if (!classMatch.Success)
                throw new Exception("The class name could not be matched.");
            ClassName = classMatch.Groups["match"].Value;


            var allKeysMatch = Regex.Match(codeHeader, "public HashSet<string> AvailableKeys { get; } = new HashSet<string> { (?<match>.+?) };");
            if (!allKeysMatch.Success)
                throw new Exception("The available keys could not be matched.");
            var availableKeys = allKeysMatch.Groups["match"].Value.Split(", ").Select(v => v.Trim('"')).ToArray();


            var defaultKeyMatch = Regex.Match(codeHeader, "private string m_defaultKey = \"(?<match>\\w+?)\";");
            if (!defaultKeyMatch.Success)
                throw new Exception("The default key could not be matched.");
            DefaultKey = defaultKeyMatch.Groups["match"].Value;


            var codeBody = code.Substring(keysStartIndex + m_separatorLine.Length);

            var codeProperties = codeBody.Split("/// <summary>\r\n");

            foreach (var codeProperty in codeProperties)
            {
                var lines = codeProperty.Split(Environment.NewLine).ToList();

                string descriptionStart = "[Description(\"";
                var descriptionLineIndex = lines.FindIndex(l => l.TrimStart().StartsWith(descriptionStart));
                if (descriptionLineIndex < 0)
                    continue;

                var property = new ResourceEntry();

                var descriptionLine = lines[descriptionLineIndex].TrimStart();
                property.Comment = ToEditorString(descriptionLine.Substring(descriptionStart.Length, descriptionLine.Length - descriptionStart.Length - 3));

                var propertyLine = lines[descriptionLineIndex + 1];
                var typeNameMatch = Regex.Match(propertyLine, "public (?<typeMatch>\\w+?) (?<nameMatch>\\w+)");
                property.ResourceName = typeNameMatch.Groups["nameMatch"].Value;
                var type = typeNameMatch.Groups["typeMatch"].Value;
                property.Type = Enum.Parse<ResourceType>(type);


                string caseStart = "case \"";
                string returnStart = "return ";

                string currKey = null;
                for (int i=descriptionLineIndex + 6; i< lines.Count; i++)
                {
                    var trimmedLine = lines[i].TrimStart();
                    if (trimmedLine.TrimStart().StartsWith(caseStart))
                    {
                        currKey = trimmedLine.Substring(caseStart.Length, trimmedLine.Length - caseStart.Length - 2);
                        continue;
                    }
                    if (trimmedLine.StartsWith(returnStart))
                    {
                        var returnToken = trimmedLine.Substring(returnStart.Length, trimmedLine.Length - returnStart.Length - 1);
                        string value;
                        string prefix;
                        switch (property.Type)
                        {
                            case ResourceType.String:
                                value = ToEditorString(returnToken.Substring(1, returnToken.Length - 2));
                                break;
                            case ResourceType.Thickness:
                                prefix = "new Thickness(";
                                value = returnToken.Substring(prefix.Length, returnToken.Length - prefix.Length - 1);
                                break;
                            case ResourceType.Visibility:
                                prefix = "Visibility.";
                                value = returnToken.Substring(prefix.Length, returnToken.Length - prefix.Length);
                                break;
                            case ResourceType.Double:
                                value = returnToken;
                                break;
                            case ResourceType.Brush:
                                prefix = "new SolidColorBrush(Color.FromArgb(";
                                value = returnToken.Substring(prefix.Length, returnToken.Length - prefix.Length - 2);
                                break;
                            case ResourceType.Color:
                                prefix = "Color.FromArgb(";
                                value = returnToken.Substring(prefix.Length, returnToken.Length - prefix.Length - 1);
                                break;
                            case ResourceType.Boolean:
                                value = returnToken;
                                break;
                            default:
                                throw new NotImplementedException($"Resource Type '{property.Type}' not supported.");
                        }

                        property.ResourceValues.Add(new ResourceKeyValue() { Key = currKey, Value = value });
                    }
                }

                Resources.Add(property);
            }
        }

        public void SaveResources(FileInfo saveAs = null)
        {
            var content = SaveResourcesToString();

            if (saveAs != null)
                ResourceFile = saveAs;

            File.WriteAllText(ResourceFile.FullName, content);
        }

        public string SaveResourcesToString()
        {
            var keys = DefinedKeys;

            StringBuilder sb = new StringBuilder();

            // usings:
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Windows;");
            sb.AppendLine("using System.Windows.Media;");
            sb.AppendLine("");

            // namespace:
            sb.AppendLine($"namespace {Namespace};");
            sb.AppendLine("");

            // class:
            sb.AppendLine($"public class {ClassName} : INotifyPropertyChanged");
            sb.AppendLine("{");

            // singleton:
            sb.AppendLine($"\tprivate static {ClassName} m_instance;");
            sb.AppendLine($"\tpublic static {ClassName} Instance => m_instance ?? (m_instance = new {ClassName}());");
            sb.AppendLine("");

            // property changed:
            sb.AppendLine("\tpublic event PropertyChangedEventHandler PropertyChanged;");
            sb.AppendLine("");

            // available keys:
            sb.AppendLine($"\tpublic HashSet<string> AvailableKeys {{ get; }} = new HashSet<string> {{ {String.Join(", ", keys.Select(k => $"\"{k}\""))} }};");
            sb.AppendLine("");

            if (DefaultKey == null)
                DefaultKey = keys.FirstOrDefault();

            sb.AppendLine($"\tprivate string CurrentLanguageKeyOrDefault()");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tvar culture = Thread.CurrentThread.CurrentCulture;");
            sb.AppendLine("\t\tif (AvailableKeys.Contains(culture.TwoLetterISOLanguageName))");
            sb.AppendLine("\t\t\treturn culture.TwoLetterISOLanguageName;");
            sb.AppendLine("\t\tif (AvailableKeys.Contains(culture.IetfLanguageTag))");
            sb.AppendLine("\t\t\treturn culture.IetfLanguageTag;");
            sb.AppendLine("\t\treturn m_defaultKey;");
            sb.AppendLine("\t}");


            // selected and default key:
            sb.AppendLine($"\tprivate string m_defaultKey = \"{DefaultKey}\";");
            sb.AppendLine($"\tprivate string m_Key = null;");
            sb.AppendLine("\tpublic string Key");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tget => m_Key ?? CurrentLanguageKeyOrDefault();");
            sb.AppendLine("\t\tset");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tif (m_Key != value)");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\tm_Key = value;");
            sb.AppendLine("\t\t\t\tPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine("");

            sb.AppendLine(m_separatorLine);
            sb.AppendLine("");

            // resource properties:
            foreach (var resource in Resources)
            {
                if (String.IsNullOrEmpty(resource.ResourceName))
                    continue;

                StringBuilder psb = new StringBuilder();
                string defaultValue = String.Empty;
                foreach (var key in keys)
                {
                    var value = resource.ResourceValues.FirstOrDefault(v => v.Key == key);
                    if (value == null)
                    {
                        value = new ResourceKeyValue() { Key = key, Value = null };
                        resource.ResourceValues.Add(value);
                    }
                    psb.AppendLine($"\t\t\t\tcase \"{key}\":");
                    if (key == DefaultKey)
                    {
                        psb.AppendLine($"\t\t\t\tdefault:");
                        defaultValue = value.Value;
                    }
                    switch (resource.Type)
                    {
                        case ResourceType.String:
                            psb.AppendLine($"\t\t\t\t\treturn \"{ToResourceClassString(value.Value)}\";");
                            break;
                        case ResourceType.Thickness:
                            psb.AppendLine($"\t\t\t\t\treturn new Thickness({value.Value});");
                            break;
                        case ResourceType.Visibility:
                            psb.AppendLine($"\t\t\t\t\treturn Visibility.{value.Value};");
                            break;
                        case ResourceType.Double:
                            psb.AppendLine($"\t\t\t\t\treturn {value.Value};");
                            break;
                        case ResourceType.Brush:
                            psb.AppendLine($"\t\t\t\t\treturn new SolidColorBrush(Color.FromArgb({value.Value}));");
                            break;
                        case ResourceType.Color:
                            psb.AppendLine($"\t\t\t\t\treturn Color.FromArgb({value.Value});");
                            break;
                        case ResourceType.Boolean:
                            psb.AppendLine($"\t\t\t\t\treturn {value.Value};");
                            break;
                        default:
                            throw new NotImplementedException($"Resource Type '{resource.Type}' not supported.");
                    }
                }

                sb.AppendLine($"\t/// <summary>");
                sb.AppendLine($"\t/// Returns a localized string that is similar to '{ToXmlString(defaultValue)}'.");
                sb.AppendLine($"\t/// </summary>");
                sb.AppendLine($"\t[Description(\"{ToResourceClassString(resource.Comment)}\")]");
                sb.AppendLine($"\tpublic {resource.Type} {resource.ResourceName}");
                sb.AppendLine("\t{");
                sb.AppendLine("\t\tget");
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tswitch (Key)");
                sb.AppendLine("\t\t\t{");
                sb.Append(psb.ToString());
                sb.AppendLine("\t\t\t}");
                sb.AppendLine("\t\t}");
                sb.AppendLine("\t}");
                sb.AppendLine("");
            }

            // close class:
            sb.AppendLine("}");

            return sb.ToString();
        }

        public static string CreateNewResourceFileForLocalizations()
        {
            var defaultEntry = new ResourceEntry() { ResourceName = "NewEntry", Type = ResourceType.String };
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "de", Value = "Neuer Wert" });
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "en", Value = "New value" });

            var manager = new ResourceManager(defaultEntry);

            return manager.SaveResourcesToString();
        }

        public static string CreateNewResourceFileForThemeStyles()
        {
            var defaultEntry = new ResourceEntry() { ResourceName = "NewEntry", Type = ResourceType.String };
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "Light", Value = "White" });
            defaultEntry.ResourceValues.Add(new ResourceKeyValue() { Key = "Dark", Value = "Black" });

            var manager = new ResourceManager(defaultEntry);

            return manager.SaveResourcesToString();
        }


        public string GetResourceNameFromValue(string resourceValueText, out List<string> initialPlaceholderValues, out string modifiedResourceValueText)
        {
            // input example 1: "Editing prices of '{Header.Name}' in {Header.Location}..."
            // input example 2: "{0} {1:has;have} {2} {2:placeholder;placeholders}"

            // first, replace all placeholders that are not numerical/pluralizations:
            // result example 1: "Editing prices of '{0}' in {1}..."
            // result example 2: (unchanged) "{0} {1:has;have} {2} {2:placeholder;placeholders}"
            Regex reg = new Regex(@"\{.+?\}");
            var brackets = reg.Matches(resourceValueText);
            List<string> placeholders = brackets.OfType<Match>().Select(b => b.Value).Distinct().ToList();
            foreach (Match bracket in brackets.OfType<Match>().OrderByDescending(m => m.Index))
            {
                // do not replace valid (numerical/pluralization) placeholders:
                if (ResourceKeyValue.PlaceholderRegex.IsMatch(bracket.Value))
                    continue;

                int index = placeholders.IndexOf(bracket.Value);

                resourceValueText = resourceValueText.Remove(bracket.Index, bracket.Length).Insert(bracket.Index, "{" + index + "}");
            }

            // remember the initial values of the placeholders, so the user can copy a complete String.Format snippet later on:
            // results in e.g. { "Header.Name", "Header.Location" }
            initialPlaceholderValues = placeholders.Select(p => p.TrimStart('{').TrimEnd('}')).ToList();


            // replace pluralization placeholders with their respective singular value:
            // result example 1: "Editing prices of '{0}' in {1}..."
            // result example 2: "{0} has {2} placeholder"
            var resourceValueTextWithSingulars = resourceValueText;
            var allPlaceholders = ResourceKeyValue.PlaceholderRegex.Matches(resourceValueTextWithSingulars);
            var pluralizationPlaceholders = allPlaceholders.Where(m => m.Value.Contains(':')).ToArray();
            foreach (var pp in pluralizationPlaceholders.OrderByDescending(p => p.Index))
            {
                resourceValueTextWithSingulars = resourceValueTextWithSingulars
                    .Remove(pp.Index, pp.Length)
                    .Insert(pp.Index, pp.Groups["singular"].Value);
            }

            // next, create a valid resource name:
            // result example 1: "EditingPricesOfIn"
            // result example 2: "HasPlaceholder"
            StringBuilder sb = new StringBuilder();
            bool nextCapital = true;
            bool isBracket = false;
            for (int i = 0; i < resourceValueTextWithSingulars.Length; i++)
            {
                var nextChar = resourceValueTextWithSingulars[i];
                if (nextChar == '{')
                    isBracket = true;
                if (nextChar == '}')
                    isBracket = false;
                if (isBracket)
                    continue;
                if (!char.IsLetterOrDigit(nextChar))
                {
                    nextCapital = true;
                    continue;
                }
                if (nextCapital)
                {
                    nextChar = char.ToUpper(nextChar);
                    nextCapital = false;
                }
                sb.Append(nextChar);
            }

            // append placeholder suffix:
            // result example 1: "EditingPricesOfIn_2S"
            // result example 2: "HasPlaceholder_3S"
            if (allPlaceholders.Any())
            {
                var placeholderCount = allPlaceholders.Select(p => p.Groups["number"].Value).Distinct().Count();
                sb.Append("_" + placeholderCount + "S");
            }

            modifiedResourceValueText = resourceValueText;
            return sb.ToString();
        }


        public string ToXmlString(string editorString)
        {
            // TODO...
            return ToResourceClassString(editorString);
        }

        public string ToResourceClassString(string editorString)
        {
            if (String.IsNullOrEmpty(editorString))
                return String.Empty;
            // replace:
            // \ backslash with \\
            // " quotes with \"
            // newline with \r\n

            string resourceClassString = editorString
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace(System.Environment.NewLine, "\\r\\n");

            return resourceClassString;
        }

        public string ToEditorString(string resourceClassString)
        {
            string editorString = resourceClassString
                .Replace("\\\\", "\\")
                .Replace("\\\"", "\"")
                .Replace("\\r\\n", System.Environment.NewLine);

            return editorString;
        }
    }
}
