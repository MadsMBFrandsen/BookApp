using BookApp.Functions;
using BookApp.Fungtions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp;

public partial class AudioEpubPage : ContentPage
{
    private ObservableCollection<string> EpubsObList { get; set; } = new ObservableCollection<string>();
    private ObservableCollection<string> EpubsStorynamesObList { get; set; } = new ObservableCollection<string>();

    private string chosenepubtitle = string.Empty;

    private List<Epub> epubs = new List<Epub>();

    //private ListView EpubFilePathListView;
    //private ListView StoryNameListView;
    private Button CompletedButton;
    private Button SelectEpubButton;
    private Button SaveButton;

    private Entry StoryNameEntry;
    private Entry StartNumberEntry;
    private Entry EndNumberEntry;

    private Label TotalNumberLabel;
    private Label CurrentNumberLabel;
    private Label TimeLeftLabel;
    private Label CurrentEpubLabel;
    private Label ChapterNameLabel;
    private Label ErrorLabel;

    private CheckBox KeepNumbersCheckBox; // Declare the CheckBox here

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

        // Wrapping ListView with Frame for Border
        var epubFilePathFrame = new Frame
        {
            BorderColor = Colors.Black,
            CornerRadius = 5,
            Padding = 0,
            Content = new ListView
            {
                ItemsSource = EpubsObList,
                Margin = new Thickness(5)
            }
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

        SelectEpubButton = new Button { Text = "Select Epub" };
        SelectEpubButton.Clicked += async (s, e) => await SelectEpub();

        CompletedButton = new Button { Text = "Done", BackgroundColor = Colors.Blue, TextColor = Colors.White };
        CompletedButton.Clicked += async (s, e) => await MakeSound();

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

        SaveButton = new Button { Text = "Save" };
        SaveButton.Clicked += async (s, e) => await SaveEditedStoryName();

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
                //progressBar.Row(9).Column(1).ColumnSpan(8), //add it to things 


                SelectEpubButton.Row(1).Column(6),
                ErrorLabel.Row(1).Column(8).ColumnSpan(3),
                //epubFilePathFrame.Row(1).RowSpan(4).Column(1).ColumnSpan(4),
                progressBar.Row(1).Column(1).ColumnSpan(4),

                TotalNumberLabel.Row(2).Column(6).ColumnSpan(2),
                new Label { Text = "Start Number" }.Row(2).Column(8),
                StartNumberEntry.Row(2).Column(9),

                CurrentNumberLabel.Row(3).Column(6).ColumnSpan(2),
                new Label { Text = "End Number" }.Row(3).Column(8),
                EndNumberEntry.Row(3).Column(9),

                TimeLeftLabel.Row(4).Column(6).ColumnSpan(2),
                keepNumbersLayout.Row(4).Column(8),

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
            PickerTitle = "Select an Epub File",
            FileTypes = customEpubFileType
        };

        var result = await FilePicker.Default.PickAsync(options);
        if (result != null)
        {
            GetContentFromEpubFile getContentFromEpubFile = new();
            List<Chapter> chapters = getContentFromEpubFile.GetContentFromEpubFunction(result.FullPath);

            Epub epubtemp = new();
            epubtemp.Filepath = result.FullPath;
            epubtemp.Title = NormalizeTitle(result.FileName).Trim();
            epubtemp.Chapters = chapters;

            if (chapters.Count >= 1)
            {
                epubs.Add(epubtemp);

                string fileNameWithoutUnderscores = NormalizeTitle(result.FileName);
                //string fileNameWithoutUnderscores = result.FileName.Replace("_", " ");
                EpubsObList.Add(result.FileName); // Change to Without _ 
                EpubsStorynamesObList.Add(fileNameWithoutUnderscores); // Change to Without _ 
            }
            else
            {
                ErrorLabel.Text = "No Chapters In Epub";
            }


        }
    }

    private async Task MakeSound()
    {
        // Disable buttons at the start of the operation
        CompletedButton.IsEnabled = false;
        SelectEpubButton.IsEnabled = false;
        SaveButton.IsEnabled = false;

        ConvertTextToSound textToSound = new();
        const double timePerWordInSeconds = 0.004;

        // Calculate total words across all EPUBs for time estimation
        int totalWordsAllEpubs = epubs.Sum(epub => epub.WordCount);
        int totalChapters = epubs.Sum(epub => epub.Chapters.Count); // Total chapters across all EPUBs
        int processedChapters = 0; // Counter for processed chapters

        // Initialize total time left based on total word count
        double totalTimeLeft = totalWordsAllEpubs * timePerWordInSeconds; // Total time in seconds

        progressBar.Progress = 0; // Reset ProgressBar

        // Set the initial values for Total Chapters and Time Left
        TotalNumberLabel.Text = "Total Chapters: " + totalChapters;
        TimeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";

        // Filter out Epubs with chapters
        int originalCount = epubs.Count; // Track the original count of epubs
        List<Epub> validEpubs = epubs.Where(e => e.Chapters.Count > 0).ToList();

        if (validEpubs.Count < originalCount) // Check if any Epub was removed
        {
            ErrorLabel.Text = "Some Epubs were removed due to having no chapters.";
        }
        else
        {
            ErrorLabel.Text = string.Empty; // Clear the error label if nothing was removed
        }

        int totalToProcessChapters = 0; // Keep track of chapters to process across all EPUBs

        // Track total time left across all EPUBs to update the TimeLeftLabel
        foreach (Epub item in validEpubs)
        {
            // Default start and end numbers
            int startNumber = item.StartNumber > 0 ? item.StartNumber : 1;
            int endNumber = item.EndNumber > 0 ? item.EndNumber : item.Chapters.Count;

            StartNumberEntry.Text = item.StartNumber.ToString();
            EndNumberEntry.Text = item.EndNumber.ToString();

            // Process only chapters within the specified range
            var chaptersToProcess = item.Chapters
                .Skip(startNumber - 1) // Skip chapters before the start number
                .Take(endNumber - startNumber + 1); // Take chapters up to the end number

            // Update total chapters to process
            totalToProcessChapters += chaptersToProcess.Count();
            TotalNumberLabel.Text = "Total Chapters: " + totalToProcessChapters; // Update the total chapters to process

            foreach (Chapter c in chaptersToProcess)
            {
                string soundfilespath = Preferences.Get("SoundFilesPath", "Error");

                CurrentNumberLabel.Text = "Current: " + c.Title;
                CurrentEpubLabel.Text = "Epub: " + item.Title;
                ChapterNameLabel.Text = c.Title;

                double chapterTime = c.WordCount * timePerWordInSeconds; // Time for this chapter
                totalTimeLeft -= chapterTime;

                // Update the remaining time across all EPUBs
                TimeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";

                await textToSound.CreateSoundFileAsync(c, item.Title, soundfilespath);

                // Update progress
                processedChapters++;
                double progress = (double)processedChapters / totalToProcessChapters; // Update progress based on total to process
                await progressBar.ProgressTo(progress, 250, Easing.Linear); // Smooth progress animation
            }
        }

        progressBar.Progress = 1; // Ensure the ProgressBar is filled at the end
        await DisplayAlert("Action", "Completed", "OK");

        // Enable buttons after the operation is completed
        CompletedButton.IsEnabled = true;
        SelectEpubButton.IsEnabled = true;
        SaveButton.IsEnabled = true;

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

                    //int Snum = epubs[epubIndex].StartNumber;
                    //int Enum = epubs[epubIndex].EndNumber;

                    //StartNumberEntry.Text = Snum.ToString();
                    //EndNumberEntry.Text = Enum.ToString();

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

    private void ResetValuesAndClearLists()
    {
        // Clear all lists
        EpubsObList.Clear();
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
    }

}
