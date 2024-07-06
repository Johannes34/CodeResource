using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeResource
{
    public class ResourceEntry : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private string m_ResourceName;
        public string ResourceName
        {
            get
            {
                return m_ResourceName;
            }
            set
            {
                if (m_ResourceName != value)
                {
                    m_ResourceName = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ResourceName)));
                }
            }
        }

        public ResourceKeyValueCollection ResourceValues { get; } = new ResourceKeyValueCollection();

        private ResourceType m_Type;
        public ResourceType Type
        {
            get
            {
                return m_Type;
            }
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Type)));
                }
            }
        }

        private string m_Comment;
        public string Comment
        {
            get
            {
                return m_Comment;
            }
            set
            {
                if (m_Comment != value)
                {
                    m_Comment = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Comment)));
                }
            }
        }

        public bool HasMissingValues => ResourceValues.Any(v => String.IsNullOrEmpty(v.Value));

        public List<string> PastedPlaceholderValues { get; set; }

        private bool m_IsErroneous;
        public bool IsErroneous
        {
            get { return m_IsErroneous; }
            set
            {
                if (value != m_IsErroneous)
                {
                    m_IsErroneous = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(IsErroneous)));
                }
            }
        }

        private string m_ErrorMessage;
        public string ErrorMessage
        {
            get { return m_ErrorMessage; }
            set
            {
                if (value != m_ErrorMessage)
                {
                    m_ErrorMessage = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
                }
            }
        }

        public bool SetValidation(bool isValid, string error = null)
        {
            IsErroneous = !isValid;
            ErrorMessage = error;
            return isValid;
        }
    }

    public class ResourceKeyValueCollection : ObservableCollection<ResourceKeyValue>
    {
        public ResourceKeyValue this[string key] => this.FirstOrDefault(k => k.Key == key);
    }

    public class ResourceKeyValue : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private string m_Key;
        public string Key
        {
            get
            {
                return m_Key;
            }
            set
            {
                if (m_Key != value)
                {
                    m_Key = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Key)));
                }
            }
        }

        private string m_Value;
        public string Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(Value)));

                    var matches = PlaceholderRegex.Matches(value ?? String.Empty);
                    HasPlaceholders = matches.Any();
                    HasPluralization = matches.Any(m => m.Value.Contains(':'));
                    PlaceholderIdentifiers = matches.Select(m =>
                    {
                        // trim {}:
                        var val = m.Value.Substring(1, m.Value.Length - 2);
                        // ignore pluralization suffix:
                        var plurIndex = val.IndexOf(':');
                        if (plurIndex >= 0)
                            val = val.Substring(0, plurIndex);
                        return val;
                    }).Distinct().ToArray();
                    
                    PlaceholderCount = PlaceholderIdentifiers.Length;

                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HasPlaceholders)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(HasPluralization)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(PlaceholderCount)));
                    PropertyChanged(this, new PropertyChangedEventArgs(nameof(PlaceholderIdentifiers)));
                }
            }
        }

        internal static Regex PlaceholderRegex = new Regex(@"{(?<number>\d+)(:(?<singular>.*?);(?<plural>.*?))?}", RegexOptions.Compiled);

        public bool HasPlaceholders { get; private set; }
        public bool HasPluralization { get; private set; }
        public int PlaceholderCount { get; private set; }
        public string[] PlaceholderIdentifiers { get; private set; }

    }
}
