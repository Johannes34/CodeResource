using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CodeResources.TestApp;

public class Resource : INotifyPropertyChanged
{
	private static Resource m_instance;
	public static Resource Instance => m_instance ?? (m_instance = new Resource());

	public event PropertyChangedEventHandler PropertyChanged;

	public HashSet<string> AvailableKeys { get; } = new HashSet<string> { "de", "en" };

	private string CurrentLanguageKeyOrDefault()
	{
		var culture = Thread.CurrentThread.CurrentCulture;
		if (AvailableKeys.Contains(culture.TwoLetterISOLanguageName))
			return culture.TwoLetterISOLanguageName;
		if (AvailableKeys.Contains(culture.IetfLanguageTag))
			return culture.IetfLanguageTag;
		return m_defaultKey;
	}
	private string m_defaultKey = "de";
	private string m_Key = null;
	public string Key
	{
		get => m_Key ?? CurrentLanguageKeyOrDefault();
		set
		{
			if (m_Key != value)
			{
				m_Key = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
			}
		}
	}

// beginning of all resource keys:

	/// <summary>
	/// Returns a localized string that is similar to 'Hallo!'.
	/// </summary>
	[Description("")]
	public String Hello
	{
		get
		{
			switch (Key)
			{
				case "de":
				default:
					return "Hallo!";
				case "en":
					return "Hello!";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Hallo {0}!'.
	/// </summary>
	[Description("")]
	public String Hello_1S
	{
		get
		{
			switch (Key)
			{
				case "de":
				default:
					return "Hallo {0}!";
				case "en":
					return "Hello {0}!";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Hallo {0} und {1}!'.
	/// </summary>
	[Description("")]
	public String HelloAnd_2S
	{
		get
		{
			switch (Key)
			{
				case "de":
				default:
					return "Hallo {0} und {1}!";
				case "en":
					return "Hello {0} and {1}!";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Hallo {0:mein Freund;meine Freunde}!'.
	/// </summary>
	[Description("")]
	public String HelloMyFriend_1S
	{
		get
		{
			switch (Key)
			{
				case "de":
				default:
					return "Hallo {0:mein Freund;meine Freunde}!";
				case "en":
					return "Hello my {0:friend;friends}!";
			}
		}
	}

}
