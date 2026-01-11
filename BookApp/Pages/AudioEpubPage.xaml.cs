using BookApp.Functions;
using BookApp.Fungtions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp;

public partial class AudioEpubPage : ContentPage
{
    private ObservableCollection<string> EpubsStorynamesObList { get; set; } = new ObservableCollection<string>();

    private string chosenepubtitle = string.Empty;

    private List<Epub> epubs = new List<Epub>();

    private Button CompletedButton;
    private Button SelectEpubButton;
    private Button SaveButton;
    private Button CleanButton;

    private Entry StoryNameEntry;
    private Entry StartNumberEntry;
    private Entry EndNumberEntry;

    private Label TotalNumberLabel;
    private Label CurrentNumberLabel;
    private Label TimeLeftLabel;
    private Label CurrentEpubLabel;
    private Label ChapterNameLabel;
    private Label ErrorLabel;

    private CheckBox KeepNumbersCheckBox; 
    private CheckBox CleanChapterFileNameCheckBox; 
    private CheckBox CleanChapterStartContentCheckBox; 

    private ProgressBar progressBar;

    public AudioEpubPage()
    {
        BindingContext = this;

        progressBar = new ProgressBar
        {
            Progress = 0, // Initial progress
            HeightRequest = 10,
            BackgroundColor = Colors.LightGray,
            ProgressColor = Colors.Blue
        };

        var storyNameFrame = new Frame
        {
            BorderColor = Colors.Black,
            CornerRadius = 5,
            Padding = 0,
            Content = new ListView
            {
                ItemsSource = EpubsStorynamesObList,
                Margin = new Thickness(5)
            }
        };

        SelectEpubButton = new Button { Text = "Select Epub", BackgroundColor = Colors.Blue, TextColor = Colors.White };
        SelectEpubButton.Clicked += async (s, e) => await SelectEpub();

        CompletedButton = new Button { Text = "Done", BackgroundColor = Colors.Green, TextColor = Colors.White };
        CompletedButton.Clicked += async (s, e) =>
        {
            CompletedButton.Background = Colors.Blue;

            await CompletedButton.ScaleTo(0.9, 50, Easing.CubicIn); // Shrink
            await CompletedButton.ScaleTo(1.1, 50, Easing.CubicOut); // Expand
            await CompletedButton.ScaleTo(1, 50, Easing.CubicInOut); // Return to normal

            await MakeSound();
        };

        CleanButton = new Button { Text = "Clean", BackgroundColor = Colors.Red, TextColor = Colors.White };
        CleanButton.Clicked += async (s, e) =>
        {
            CleanButton.BackgroundColor = Colors.Blue;
            // Animate the button (scale down and up for a "pop" effect)
            await CleanButton.ScaleTo(0.9, 50, Easing.CubicIn); // Shrink
            await CleanButton.ScaleTo(1.1, 50, Easing.CubicOut); // Expand
            await CleanButton.ScaleTo(1, 50, Easing.CubicInOut); // Return to normal

            await Task.Delay(1000);

            CleanButton.BackgroundColor = Colors.Red;

            await ResetValuesAndClearLists();
        };

        SaveButton = new Button { Text = "Save", BackgroundColor = Colors.LightBlue, TextColor = Colors.White };
        SaveButton.Clicked += async (s, e) =>
        {
            SaveButton.BackgroundColor = Colors.Blue;

            await SaveButton.ScaleTo(0.9, 50, Easing.CubicIn); // Shrink
            await SaveButton.ScaleTo(1.1, 50, Easing.CubicOut); // Expand
            await SaveButton.ScaleTo(1, 50, Easing.CubicInOut); // Return to normal

            await Task.Delay(500);

            SaveButton.BackgroundColor = Colors.LightBlue;

            await SaveEditedStoryName();
        };

        StoryNameEntry = new Entry { Placeholder = "Story Name" };
        StartNumberEntry = new Entry { Placeholder = "Start Number", Text = "0", Keyboard = Keyboard.Numeric };
        EndNumberEntry = new Entry { Placeholder = "End Number", Text = "0", Keyboard = Keyboard.Numeric };

        TotalNumberLabel = new Label { Text = "Total: Na" };
        CurrentNumberLabel = new Label { Text = "Current: Na" };
        TimeLeftLabel = new Label { Text = "Time Left: Na" };
        CurrentEpubLabel = new Label { Text = "Current Epub: Na" };
        ChapterNameLabel = new Label { Text = "Chapter Name: Na" };
        ErrorLabel = new Label { Text = "Error", TextColor = Colors.Red };

        // Create and initialize the KeepNumbersCheckBox properly
        KeepNumbersCheckBox = new CheckBox { IsChecked = true };

        // CheckBox with accompanying Label
        var keepNumbersLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                KeepNumbersCheckBox,
                new Label { Text = "Keep Numbers", VerticalOptions = LayoutOptions.Center }
            }
        };

        CleanChapterFileNameCheckBox = new CheckBox { IsChecked = false };

        var cleanChapterFileNameLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                CleanChapterFileNameCheckBox,
                new Label { Text = "Clean Filename", VerticalOptions = LayoutOptions.Center }
            }
        };

        CleanChapterStartContentCheckBox = new CheckBox { IsChecked = false };

        var cleanChapterStartContentLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                CleanChapterStartContentCheckBox,
                new Label { Text = "Clean Content Chapter Repeat", VerticalOptions = LayoutOptions.Center }
            }
        };
        // Handle item tapped
        var storyNameListView = storyNameFrame.Content as ListView;
        storyNameListView.ItemTapped += async (s, e) =>
        {
            var tappedItem = e.Item as string;
            await OnStoryNameItemTapped(tappedItem);
        };

        Content = new Grid
        {
            RowDefinitions = Rows.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),
            ColumnDefinitions = Columns.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),

            Children =
            {
                SelectEpubButton.Row(1).Column(6),
                CleanButton.Row(1).Column(7),
                ErrorLabel.Row(1).Column(8).ColumnSpan(3),
                progressBar.Row(1).RowSpan(2).Column(1).ColumnSpan(4),

                TotalNumberLabel.Row(2).Column(6).ColumnSpan(2),
                new Label { Text = "Start Number" }.Row(2).Column(8),
                StartNumberEntry.Row(2).Column(9),

                CurrentNumberLabel.Row(3).Column(6).ColumnSpan(2),
                new Label { Text = "End Number" }.Row(3).Column(8),
                EndNumberEntry.Row(3).Column(9),

                TimeLeftLabel.Row(4).Column(6).ColumnSpan(2),

                keepNumbersLayout.Row(4).Column(8),
                // need more space
                //cleanChapterFileNameLayout.Row(5).Column(8),
                //cleanChapterStartContentLayout.Row(6).Column(8),

                CurrentEpubLabel.Row(5).Column(6).ColumnSpan(3),
                storyNameFrame.Row(5).RowSpan(4).Column(1).ColumnSpan(4),

                new Label { Text = "Storyname" }.Row(6).Column(6),
                StoryNameEntry.Row(6).Column(7).ColumnSpan(3),

                new Label { Text = "ChapterName" }.Row(7).Column(6),
                ChapterNameLabel.Row(7).Column(7).ColumnSpan(3),

                SaveButton.Row(8).RowSpan(2).Column(6),
                CompletedButton.Row(8).RowSpan(2).Column(8).ColumnSpan(2)
            }
        };
    }

    private async Task SelectEpub()
    {
        var customEpubFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.iOS, new[] { "org.idpf.epub-container" } }, // iOS Uniform Type Identifier (UTI)
        { DevicePlatform.Android, new[] { "application/epub+zip" } }, // Android MIME type
        { DevicePlatform.WinUI, new[] { ".epub" } },                 // Windows file extension
        { DevicePlatform.MacCatalyst, new[] { "org.idpf.epub-container" } } // Mac UTI
    });

        var options = new PickOptions
        {
            PickerTitle = "Select Epub Files",   // Custom title for the picker
            FileTypes = customEpubFileType     // Allow only EPUB files based on custom file types
        };

        var results = await FilePicker.Default.PickMultipleAsync(options);  // Use PickMultipleAsync for multiple file selection

        if (results != null && results.Any())
        {
            foreach (var result in results)
            {
                GetContentFromEpubFile getContentFromEpubFile = new();
                List<Chapter> chapters = getContentFromEpubFile.GetContentFromEpubFunction(result.FullPath);

                Epub epubtemp = new();
                epubtemp.Filepath = result.FullPath;
                epubtemp.Title = NormalizeTitle(result.FileName).Trim();
                epubtemp.Chapters = chapters;
                epubtemp.EndNumber = chapters.Count;

                if (chapters.Count >= 1)
                {
                    epubs.Add(epubtemp);

                    string fileNameWithoutUnderscores = NormalizeTitle(result.FileName);
                    EpubsStorynamesObList.Add(fileNameWithoutUnderscores); // Change to Without _ 
                }
                else
                {
                    ErrorLabel.Text = "No Chapters In Epub";
                }
            }
        }
        else
        {
            ErrorLabel.Text = "No files selected.";
        }
    }


    private async Task MakeSound()
    {
        CompletedButton.IsEnabled = false;
        SelectEpubButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        CleanButton.IsEnabled = false;

        var textToSound = new ConvertTextToSound();
        string soundfilespath = Preferences.Get("SoundFilesPath", "Error");
        string textfilespath = Preferences.Get("TextFilesPath", "Error");

        double timePerWordInSeconds = Convert.ToDouble(Preferences.Get("TimePerWord", "0.004"));
        if (timePerWordInSeconds.ToString().Length > 6)
            timePerWordInSeconds = 0.004;

        int totalWordsAllEpubs = epubs.Sum(epub => epub.WordCount);
        int totalChapters = epubs.Sum(epub => epub.Chapters.Count);
        int processedChapters = 0;
        double totalTimeLeft = totalWordsAllEpubs * timePerWordInSeconds;

        progressBar.Progress = 0;
        TotalNumberLabel.Text = "Total Chapters: " + totalChapters;
        TimeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";

        List<Epub> validEpubs = epubs.Where(e => e.Chapters.Count > 0).ToList();

        if (validEpubs.Count < epubs.Count)
            ErrorLabel.Text = "Some Epubs were removed due to having no chapters.";
        else
            ErrorLabel.Text = string.Empty;

        int totalToProcessChapters = 0;

        foreach (Epub item in validEpubs)
        {
            int startNumber = item.StartNumber > 0 ? item.StartNumber : 1;
            int endNumber = item.EndNumber > 0 ? item.EndNumber : item.Chapters.Count;

            StartNumberEntry.Text = item.StartNumber.ToString();
            EndNumberEntry.Text = item.EndNumber.ToString();

            var chaptersToProcess = item.Chapters
                .Skip(startNumber - 1)
                .Take(endNumber - startNumber + 1);

            totalToProcessChapters += chaptersToProcess.Count();
            TotalNumberLabel.Text = "Total Chapters: " + totalToProcessChapters;

            // SAFE folder name for story text files
            string safeStoryFolderName = ConvertTextToSound.SafeFileName(item.Title);

            foreach (Chapter c in chaptersToProcess) // Chapter from BookApp.Models
            {
                CurrentNumberLabel.Text = "Current: " + c.Title;
                CurrentEpubLabel.Text = "Epub: " + item.Title;
                ChapterNameLabel.Text = c.Title;

                double chapterTime = c.WordCount * timePerWordInSeconds;
                totalTimeLeft -= chapterTime;
                TimeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";

                // Sound file generation
                await textToSound.CreateSoundFileAsync(c, item.Title, soundfilespath);

                // Text file generation (SAFE FILENAMES)
                if (textfilespath != "Error")
                {
                    string bookFolder = System.IO.Path.Combine(textfilespath, safeStoryFolderName);
                    System.IO.Directory.CreateDirectory(bookFolder);

                    string safeChapterName = ConvertTextToSound.SafeFileName(c.Title);
                    string textFilePath = System.IO.Path.Combine(bookFolder, safeChapterName + ".txt");
                    if (!System.IO.File.Exists(textFilePath))
                    {
                        await System.IO.File.WriteAllTextAsync(textFilePath, c.Content ?? "[No content]", Encoding.UTF8);
                    }
                }

                // Update progress
                processedChapters++;
                double progress = (double)processedChapters / totalToProcessChapters;
                await progressBar.ProgressTo(progress, 250, Easing.Linear);
            }
        }

        progressBar.Progress = 1;
        await DisplayAlert("Action", "Completed", "OK");

        CompletedButton.IsEnabled = true;
        SelectEpubButton.IsEnabled = true;
        SaveButton.IsEnabled = true;
        CleanButton.IsEnabled = true;

        ResetValuesAndClearLists();
    }



    // Set the ListView item clicked to StoryNameEntry for editing
    public async Task OnStoryNameItemTapped(string tappedItem)
    {
        if (tappedItem != null)
        {
            StoryNameEntry.Text = tappedItem; // Populate StoryNameEntry with the tapped item text
            chosenepubtitle = tappedItem;

            int epubIndex = epubs.FindIndex(item => NormalizeTitle(item.Title).ToLower() == NormalizeTitle(chosenepubtitle).ToLower());
            if (epubIndex >= 0)
            {
                int Snum = epubs[epubIndex].StartNumber;
                int Enum = epubs[epubIndex].EndNumber;

                StartNumberEntry.Text = Snum.ToString();
                EndNumberEntry.Text = Enum.ToString();
            }
        }
    }

    // Method to save the edited story name
    public async Task SaveEditedStoryName()
    {
        string editedStoryName = StoryNameEntry.Text;
        editedStoryName = System.Text.RegularExpressions.Regex.Replace(editedStoryName ?? "", @"\s+", " ").Trim();



        // Check if the edited story name is valid (not null or empty)
        if (!string.IsNullOrEmpty(editedStoryName))
        {
            // Get the current story name from the chosen title
            var currentStoryName = chosenepubtitle;

            // Find the index of the current item in the ObservableCollection (ListView's source)
            int index = EpubsStorynamesObList.IndexOf(currentStoryName);

            if (index >= 0)
            {
                // Update the item in the ObservableCollection with the new edited value
                EpubsStorynamesObList[index] = editedStoryName.Trim();  // Automatically refreshes the ListView

                // Find the Epub in the epubs list and overwrite it
                int epubIndex = epubs.FindIndex(item => NormalizeTitle(item.Title).ToLower() == NormalizeTitle(currentStoryName).ToLower());
                if (epubIndex >= 0)
                {
                    int Snum = Convert.ToInt32(StartNumberEntry.Text);
                    int Enum = Convert.ToInt32(EndNumberEntry.Text);

                    StartNumberEntry.Text = "0";
                    EndNumberEntry.Text = "0";

                    // Overwrite the existing Epub with a new instance
                    epubs[epubIndex] = new Epub
                    {
                        Filepath = epubs[epubIndex].Filepath,
                        Title = editedStoryName.Trim(),
                        Description = epubs[epubIndex].Description,
                        Author = epubs[epubIndex].Author,
                        Chapters = epubs[epubIndex].Chapters,
                        StartNumber = Snum,
                        EndNumber = Enum,
                        NeedStartNumbers = KeepNumbersCheckBox.IsChecked
                    };
                    if (epubs[epubIndex].Chapters.Count < 1)
                    {
                        // Remove the Epub from both collections
                        EpubsStorynamesObList.RemoveAt(index);
                        epubs.RemoveAt(epubIndex);

                        // Update the ErrorLabel to indicate the removal
                        ErrorLabel.Text = "Epub removed due to no chapters.";
                    }
                }
            }
        }
        else
        {
            // Handle empty input if needed (optional)
            await DisplayAlert("Error", "Please enter a valid story name", "OK");
        }
    }

    // Normalization function
    private string NormalizeTitle(string title)
    {
        // Replace underscores with spaces, or vice versa, and convert to lowercase
        if (title.Contains(".epub"))
        {
            title = title.Replace(".epub", "");
        }
        return title.Replace("_", " ");
    }

    private async Task ResetValuesAndClearLists()
    {
        // Clear all lists        
        EpubsStorynamesObList.Clear();
        epubs.Clear();

        // Reset all Entry fields
        StoryNameEntry.Text = string.Empty;
        StartNumberEntry.Text = "0";
        EndNumberEntry.Text = "0";

        // Reset Labels
        TotalNumberLabel.Text = "Total: Na";
        CurrentNumberLabel.Text = "Current: Na";
        TimeLeftLabel.Text = "Time Left: Na";
        CurrentEpubLabel.Text = "Current Epub: Na";
        ChapterNameLabel.Text = "Chapter Name: Na";
        ErrorLabel.Text = "Error";

        // Reset CheckBox
        KeepNumbersCheckBox.IsChecked = true;

        // Reset ProgressBar
        progressBar.Progress = 0;

        // Reste Buttons
        CompletedButton.Background = Colors.Green;
    }

    private void Enablebutton()
    {
        CompletedButton.IsEnabled = true;
        SelectEpubButton.IsEnabled = true;
        SaveButton.IsEnabled = true;
        CleanButton.IsEnabled = true;

        //Color orginal
    }

    private void Disablebutton()
    {
        CompletedButton.IsEnabled = false;
        SelectEpubButton.IsEnabled = false;
        SaveButton.IsEnabled = false;
        CleanButton.IsEnabled = false;

        //Color Gray
    }

}
