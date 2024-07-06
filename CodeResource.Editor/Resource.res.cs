using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CodeResource.Editor;

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
	private string m_defaultKey = "en";
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
	/// Returns a localized string that is similar to '_File'.
	/// </summary>
	[Description("Top menu item. Should begin with underscore.")]
	public String Menu_File
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Datei";
				case "en":
				default:
					return "_File";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '_Language'.
	/// </summary>
	[Description("Top menu item. Should begin with underscore.")]
	public String Menu_Language
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Sprache";
				case "en":
				default:
					return "_Language";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '_New'.
	/// </summary>
	[Description("File menu item. Should begin with underscore.")]
	public String Menu_New
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Neu";
				case "en":
				default:
					return "_New";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '_Open'.
	/// </summary>
	[Description("File menu item. Should begin with underscore.")]
	public String Menu_Open
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Öffnen";
				case "en":
				default:
					return "_Open";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '_Save'.
	/// </summary>
	[Description("File menu item. Should begin with underscore.")]
	public String Menu_Save
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Speichern";
				case "en":
				default:
					return "_Save";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '_Exit'.
	/// </summary>
	[Description("File menu item. Should begin with underscore.")]
	public String Menu_Exit
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "_Beenden";
				case "en":
				default:
					return "_Exit";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Settings'.
	/// </summary>
	[Description("")]
	public String Settings
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Einstellungen";
				case "en":
				default:
					return "Settings";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Filter'.
	/// </summary>
	[Description("")]
	public String Filter
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Filter";
				case "en":
				default:
					return "Filter";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Show only entries with missing localization'.
	/// </summary>
	[Description("")]
	public String ShowOnlyEntriesWithMissingLocalization
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Zeige nur Einträge mit fehlender Übersetzung";
				case "en":
				default:
					return "Show only entries with missing localization";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Resource Name'.
	/// </summary>
	[Description("")]
	public String ResourceName
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Ressourcen Name";
				case "en":
				default:
					return "Resource Name";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Type'.
	/// </summary>
	[Description("")]
	public String Type
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Typ";
				case "en":
				default:
					return "Type";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Comment'.
	/// </summary>
	[Description("")]
	public String Comment
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Kommentar";
				case "en":
				default:
					return "Comment";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Copy'.
	/// </summary>
	[Description("")]
	public String Copy
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Kopieren";
				case "en":
				default:
					return "Copy";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'There {0:is;are} {0} resource {0:entry;entries} with the same name.'.
	/// </summary>
	[Description("")]
	public String ThereIsResourceEntryWithTheSameName_1S
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Es gibt {0} anderen Ressourcen-{0:Eintrag;Einträge} mit dem selben Namen.";
				case "en":
				default:
					return "There {0:is;are} {0} resource {0:entry;entries} with the same name.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The Resource Name must be set.'.
	/// </summary>
	[Description("")]
	public String TheResourceNameMustBeSet
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Ressourcenname muss festgelegt werden.";
				case "en":
				default:
					return "The Resource Name must be set.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The Resource Name has an illegal format. It may only contain letters, digits and underscores and must not start with a digit.'.
	/// </summary>
	[Description("")]
	public String TheResourceNameHasAnIllegalFormatItMayOnlyContainLettersDigitsAndUnderscoresAndMustNotStartWithADigit
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Ressourcenname hat ein ungültiges Format. Er darf nur Buchstaben, Ziffern und Unterstriche enthalten und darf nicht mit einer Ziffer beginnen.";
				case "en":
				default:
					return "The Resource Name has an illegal format. It may only contain letters, digits and underscores and must not start with a digit.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The Resource Name ends with a placeholder suffix, but there are no placeholders in the value.'.
	/// </summary>
	[Description("")]
	public String TheResourceNameEndsWithAPlaceholderSuffixButThereAreNoPlaceholdersInTheValue
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Ressourcenname endet mit einem Platzhalter-Suffix, aber es gibt keine Platzhalter im Wert.";
				case "en":
				default:
					return "The Resource Name ends with a placeholder suffix, but there are no placeholders in the value.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The Resource Name ends with {0}, but is expected to end with _{1}S.'.
	/// </summary>
	[Description("")]
	public String TheResourceNameEndsWithButIsExpectedToEndWithS_2S
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Ressourcenname endet mit {0}, sollte jedoch mit _{1}S enden.";
				case "en":
				default:
					return "The Resource Name ends with {0}, but is expected to end with _{1}S.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The Resource Name is expected to end with the suffix '_{0}S' indicating the number of placeholders.'.
	/// </summary>
	[Description("")]
	public String TheResourceNameIsExpectedToEndWithTheSuffixSIndicatingTheNumberOfPlaceholders_1S
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Ressourcenname soll mit dem Suffix '_{0}S' enden, was die Anzahl der Platzhalter angibt.";
				case "en":
				default:
					return "The Resource Name is expected to end with the suffix '_{0}S' indicating the number of placeholders.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'The value for the default localization '{0}' is missing.'.
	/// </summary>
	[Description("")]
	public String TheValueForTheDefaultLocalizationIsMissing_1S
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Der Wert für die Standardlokalisierung '{0}' fehlt.";
				case "en":
				default:
					return "The value for the default localization '{0}' is missing.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to 'Not all values have the same amount of placeholders.'.
	/// </summary>
	[Description("")]
	public String NotAllValuesHaveTheSameAmountOfPlaceholders
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "Nicht alle Werte haben die gleiche Anzahl von Platzhaltern.";
				case "en":
				default:
					return "Not all values have the same amount of placeholders.";
			}
		}
	}

	/// <summary>
	/// Returns a localized string that is similar to '{0} {1:has;have} {2} {2:placeholder;placeholders}'.
	/// </summary>
	[Description("")]
	public String HasPlaceholders_3S
	{
		get
		{
			switch (Key)
			{
				case "de":
					return "{0} {1:hat;haben} {2} {2:Platzhalter;Platzhalter}";
				case "en":
				default:
					return "{0} {1:has;have} {2} {2:placeholder;placeholders}";
			}
		}
	}

}
