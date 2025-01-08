using CommunityToolkit.Maui.Storage;

namespace BookApp;

public partial class ConfigPage : ContentPage
{
    public ConfigPage()
    {
        InitializeComponent();
        LoadConfig();
    }

    private async void LoadConfig()
    {
        // Load saved configuration or set default paths
        TextFilesPathEntry.Text = Preferences.Get("TextFilesPath", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/TextFiles");
        SoundFilesPathEntry.Text = Preferences.Get("SoundFilesPath", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/SoundFiles");
        //EpubDefaultPathEntry.Text = Preferences.Get("EpubDefaultPath", FileSystem.AppDataDirectory + "/ePubFiles");
#if ANDROID
        EpubDefaultPathEntry.Text = Preferences.Get(
            "EpubDefaultPath",
            Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath
        );
#elif IOS
EpubDefaultPathEntry.Text = Preferences.Get(
    "EpubDefaultPath",
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Downloads")
);
#else
EpubDefaultPathEntry.Text = Preferences.Get(
    "EpubDefaultPath",
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
);
#endif
    }

    private async void OnTextFilesPathClicked(object sender, EventArgs e)
    {
        var folderResult = await FolderPicker.Default.PickAsync();
        if (folderResult.IsSuccessful)
        {
            TextFilesPathEntry.Text = folderResult.Folder.Path;
        }
    }

    private async void OnSoundFilesPathClicked(object sender, EventArgs e)
    {
        var folderResult = await FolderPicker.Default.PickAsync();
        if (folderResult.IsSuccessful)
        {
            SoundFilesPathEntry.Text = folderResult.Folder.Path;
        }
    }

    private async void OnEpubPathClicked(object sender, EventArgs e)
    {
        var folderResult = await FolderPicker.Default.PickAsync();
        if (folderResult.IsSuccessful)
        {
            EpubDefaultPathEntry.Text = folderResult.Folder.Path;
        }
    }

    private void OnSaveConfigClicked(object sender, EventArgs e)
    {
        // Save the paths in Preferences
        Preferences.Set("TextFilesPath", TextFilesPathEntry.Text);
        Preferences.Set("SoundFilesPath", SoundFilesPathEntry.Text);
        Preferences.Set("EpubDefaultPath", EpubDefaultPathEntry.Text);

        DisplayAlert("Configuration Saved", "Your configuration has been saved successfully.", "OK");
    }
}