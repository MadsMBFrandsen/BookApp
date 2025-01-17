using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using System.Speech.Synthesis;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp.Pages;

public partial class ConfigV2 : ContentPage
{
    private Entry _textFilesPathEntry;
    private Entry _soundFilesPathEntry;
    private Entry _epubDefaultPathEntry;
    private Entry _libraryFolderPathEntry;
    private Button _selectTextFolderButton;
    private Button _selectSoundFolderButton;
    private Button _selectEpubFolderButton;
    private Button _selectLibraryFolderButton;
    private Button _saveConfigButton;
    private Label _textFilesLabel;
    private Label _soundFilesLabel;
    private Label _epubDefaultPathLabel;
    private Label _libraryFolderPathLabel;
    private Label _zillaStatusLabel;

    public ConfigV2()
    {
        Title = "Configuration";
        const string DefaultFolderName = "MyAppData";

        string defaultTextFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultFolderName, "TextFiles");
        string defaultSoundFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultFolderName, "SoundFiles");
        string defaultLibraryFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DefaultFolderName, "Library");
        string defaultEpubPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

        // Initialize the zilla label
        _zillaStatusLabel = new Label { Text = "Checking for Microsoft Zira Desktop..." };

        // Check if Microsoft Zira Desktop is installed
        IsMicrosoftZiraDesktopInstalled();

        // Initialize path entries
        _textFilesPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for text files" }
            .Text(Preferences.Get("TextFilesPath", defaultTextFilesPath));
        _soundFilesPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for sound files" }
            .Text(Preferences.Get("SoundFilesPath", defaultSoundFilesPath));
        _epubDefaultPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for ePub files" }
            .Text(Preferences.Get("EpubDefaultPath", defaultEpubPath));
        _libraryFolderPathEntry = new Entry { IsReadOnly = true, Placeholder = "Select folder for library" }
            .Text(Preferences.Get("LibraryFolderPath", defaultLibraryFolderPath));

        // Initialize buttons
        _selectTextFolderButton = new Button { Text = "Select Text Folder" };
        _selectSoundFolderButton = new Button { Text = "Select Sound Folder" };
        _selectEpubFolderButton = new Button { Text = "Select Epub Folder" };
        _selectLibraryFolderButton = new Button { Text = "Select Library Folder" };
        _saveConfigButton = new Button { Text = "Save Configuration", BackgroundColor = Colors.Green, TextColor = Colors.White };

        // Button click events
        _selectTextFolderButton.Clicked += async (s, e) => await OnTextFilesPathClicked();
        _selectSoundFolderButton.Clicked += async (s, e) => await OnSoundFilesPathClicked();
        _selectEpubFolderButton.Clicked += async (s, e) => await OnEpubPathClicked();
        _selectLibraryFolderButton.Clicked += async (s, e) => await OnLibraryFolderPathClicked();
        _saveConfigButton.Clicked += OnSaveConfigClicked;

        // Initialize labels
        _textFilesLabel = new Label { Text = "Text Files Path" };
        _soundFilesLabel = new Label { Text = "Sound Files Path" };
        _epubDefaultPathLabel = new Label { Text = "Epub Default Path" };
        _libraryFolderPathLabel = new Label { Text = "Library Folder Path" };

        // Set up the grid layout
        Content = new Grid
        {
            RowDefinitions = Rows.Define(Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto),
            ColumnDefinitions = Columns.Define(Star, Star, Star, Star),
            Children =
            {
                _zillaStatusLabel.Row(0).Column(0).ColumnSpan(4),
                _textFilesLabel.Row(1).Column(0),
                _textFilesPathEntry.Row(1).Column(1).ColumnSpan(2),
                _selectTextFolderButton.Row(1).Column(3),

                _soundFilesLabel.Row(2).Column(0),
                _soundFilesPathEntry.Row(2).Column(1).ColumnSpan(2),
                _selectSoundFolderButton.Row(2).Column(3),

                _epubDefaultPathLabel.Row(3).Column(0),
                _epubDefaultPathEntry.Row(3).Column(1).ColumnSpan(2),
                _selectEpubFolderButton.Row(3).Column(3),

                _libraryFolderPathLabel.Row(4).Column(0),
                _libraryFolderPathEntry.Row(4).Column(1).ColumnSpan(2),
                _selectLibraryFolderButton.Row(4).Column(3),

                _saveConfigButton.Row(5).Column(0).ColumnSpan(4),
            }
        };
    }

    private async Task OnTextFilesPathClicked()
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();
        if (folderPickerResult.IsSuccessful)
        {
            _textFilesPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private async Task OnSoundFilesPathClicked()
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();
        if (folderPickerResult.IsSuccessful)
        {
            _soundFilesPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private async Task OnEpubPathClicked()
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();
        if (folderPickerResult.IsSuccessful)
        {
            _epubDefaultPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private async Task OnLibraryFolderPathClicked()
    {
        var folderPickerResult = await FolderPicker.Default.PickAsync();
        if (folderPickerResult.IsSuccessful)
        {
            _libraryFolderPathEntry.Text = folderPickerResult.Folder.Path;
        }
    }

    private void OnSaveConfigClicked(object sender, EventArgs e)
    {
        // Save paths to Preferences
        Preferences.Set("TextFilesPath", _textFilesPathEntry.Text);
        Preferences.Set("SoundFilesPath", _soundFilesPathEntry.Text);
        Preferences.Set("EpubDefaultPath", _epubDefaultPathEntry.Text);
        Preferences.Set("LibraryFolderPath", _libraryFolderPathEntry.Text);

        // Display a success message
        DisplayAlert("Configuration Saved", "Folder paths have been successfully saved.", "OK");
    }

    private void IsMicrosoftZiraDesktopInstalled()
    {
        // Initialize the SpeechSynthesizer to access installed voices
        using (var synthesizer = new SpeechSynthesizer())
        {
            // Get the list of installed voices
            var installedVoices = synthesizer.GetInstalledVoices();

            // Check if Microsoft Zira Desktop is installed
            foreach (var voice in installedVoices)
            {
                if (voice.VoiceInfo.Name.Equals("Microsoft Zira Desktop", StringComparison.OrdinalIgnoreCase))
                {
                    _zillaStatusLabel.Text = "Microsoft Zira Desktop is installed."; // Update label text when installed
                    _zillaStatusLabel.TextColor = Colors.Green;
                    return; // Exit once found
                }
            }
        }

        // If not found, update the label text
        _zillaStatusLabel.Text = "Microsoft Zira Desktop is not installed.";
        _zillaStatusLabel.TextColor = Colors.Red;
    }
}