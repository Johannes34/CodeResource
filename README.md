# CodeResource
Alternative C# code-based resources to the good old resx, including a convenient editor and auto-localization using ChatGPT.

### Background:
The currently wide-used resx have some drawbacks
- They generate satellite assemblies upon compilation, which are loosley file-based coupled with the entry assembly at runtime. This may make it complicated to package or store assemblies in some scenarios.
- The Visual Studio resx editor is slow, outdated and each language needs to be opened separately
- The generated class does not implement INotifyPropertyChanged

### Features in Core:
- all resources are stored in a single code-behind .res.cs code file and compiled with the project, so there are no satellite assemblies
- Property Changed pattern on the generated code class allows seemlessly switching language during runtime
- supports placeholder patterns for easier pluralizations and formatting

### Features in Editor:
- Auto-generates resource keys from the first inserted localization
- Copy screen with C# and XAML templates
- filtering
- validations during input
- easily auto-localize missing languages with ChatGPT
- available as Standalone editor App and as WPF-control
- Visual Studio Extension coming soon
