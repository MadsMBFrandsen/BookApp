using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;

namespace BookApp;

public partial class ConfigPage : ContentPage
{
    private readonly Entry _textFilesPathEntry;
    private readonly Entry _soundFilesPathEntry;
    private readonly Entry _epubDefaultPathEntry;

    public ConfigPage()
    {
        Title = "Configuration";
        const string DefaultFolderName = "MyAppData";

        // Build the default paths for text files and sound files
        string defaultTextFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultFolderName, "TextFiles");
        string defaultSoundFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultFolderName, "SoundFiles");
        string defaultEpubPath;

        // Handle platform-specific default paths
#if ANDROID
        defaultEpubPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "Download");
#else
        defaultEpubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
#endif

        // Ensure the folders exist
        Directory.CreateDirectory(defaultTextFilesPath);
        Directory.CreateDirectory(defaultSoundFilesPath);

        // Initialize the Entry fields
        _textFilesPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for text files" }
            .Text(Preferences.Get("TextFilesPath", defaultTextFilesPath));
        _soundFilesPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for sound files" }
            .Text(Preferences.Get("SoundFilesPath", defaultSoundFilesPath));
        _epubDefaultPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for ePub files" }
            .Text(Preferences.Get("EpubDefaultPath", defaultEpubPath));

        Content = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 15,

            Children =
            {
                new Label { Text = "Text Files Path" },
                _textFilesPathEntry,
                new Button { Text = "Choose Text Path" }
                    .Invoke(button => button.Clicked += OnTextFilesPathClicked),

                new Label { Text = "Sound Files Path" },
                _soundFilesPathEntry,
                new Button { Text = "Choose Sound Path" }
                    .Invoke(button => button.Clicked += OnSoundFilesPathClicked),

                new Label { Text = "Epub Default Path" },
                _epubDefaultPathEntry,
                new Button { Text = "Choose Epub Path" }
                    .Invoke(button => button.Clicked += OnEpubPathClicked),

                new Button { Text = "Save Configuration", BackgroundColor = Colors.Green, TextColor = Colors.White }
                    .Center()
                    .Margin(10)
                    .Invoke(button => button.Clicked += OnSaveConfigClicked)
            }
        };
    }

    private async void OnTextFilesPathClicked(object sender, EventArgs e)
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();

        if (folderPickerResult.IsSuccessful)
        {
            _textFilesPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private async void OnSoundFilesPathClicked(object sender, EventArgs e)
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();

        if (folderPickerResult.IsSuccessful)
        {
            _soundFilesPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private async void OnEpubPathClicked(object sender, EventArgs e)
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();

        if (folderPickerResult.IsSuccessful)
        {
            _epubDefaultPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private void OnSaveConfigClicked(object sender, EventArgs e)
    {
        // Save paths to Preferences
        Preferences.Set("TextFilesPath", _textFilesPathEntry.Text);
        Preferences.Set("SoundFilesPath", _soundFilesPathEntry.Text);
        Preferences.Set("EpubDefaultPath", _epubDefaultPathEntry.Text);

        // Display a success message
        DisplayAlert("Configuration Saved", "Folder paths have been successfully saved.", "OK");
    }
}