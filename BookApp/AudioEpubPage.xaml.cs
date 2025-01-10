using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp;

public partial class AudioEpubPage : ContentPage
{
    private ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();

    private ListView EpubFilePathListView;
    private ListView StoryNameListView;
    private Button CompletedButton;
    private Button SelectEpubButton;
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
    private Button SaveButton;

    public AudioEpubPage()
    {
        BindingContext = this;

        // Wrapping ListView with Frame for Border
        var epubFilePathFrame = new Frame
        {
            BorderColor = Colors.Black,
            CornerRadius = 5,
            Padding = 0,
            Content = new ListView
            {
                ItemsSource = Items,
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
                ItemsSource = Items,
                Margin = new Thickness(5)
            }
        };

        SelectEpubButton = new Button { Text = "Select Epub" };
        SelectEpubButton.Clicked += async (s, e) => await SelectEpub();

        CompletedButton = new Button { Text = "Done", BackgroundColor = Colors.Blue, TextColor = Colors.White };
        CompletedButton.Clicked += async (s, e) => await CompleteTask();

        StoryNameEntry = new Entry { Placeholder = "Story Name" };
        StartNumberEntry = new Entry { Placeholder = "Start Number", Text = "0", Keyboard = Keyboard.Numeric };
        EndNumberEntry = new Entry { Placeholder = "End Number", Text = "0", Keyboard = Keyboard.Numeric };

        TotalNumberLabel = new Label { Text = "Total: Na" };
        CurrentNumberLabel = new Label { Text = "Current: Na" };
        TimeLeftLabel = new Label { Text = "Time Left: Na" };
        CurrentEpubLabel = new Label { Text = "Current Epub: Na" };
        ChapterNameLabel = new Label { Text = "Chapter Name: Na" };
        ErrorLabel = new Label { Text = "Error", TextColor = Colors.Red };

        // CheckBox with accompanying Label
        var keepNumbersLayout = new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new CheckBox { IsChecked = true },
                new Label { Text = "Keep Numbers", VerticalOptions = LayoutOptions.Center }
            }
        };

        SaveButton = new Button { Text = "Save" };
        SaveButton.Clicked += async (s, e) => await SaveEditedStoryName();

        // Handle item tapped
        var storyNameListView = storyNameFrame.Content as ListView;
        storyNameListView.ItemTapped += async (s, e) =>
        {
            await OnStoryNameItemTapped(e.Item as string);
        };

        Content = new Grid
        {
            RowDefinitions = Rows.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),
            ColumnDefinitions = Columns.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),

            Children =
            {
                SelectEpubButton.Row(1).Column(6),
                ErrorLabel.Row(1).Column(8).ColumnSpan(3),
                epubFilePathFrame.Row(1).RowSpan(4).Column(1).ColumnSpan(4),

                TotalNumberLabel.Row(2).Column(6).ColumnSpan(2),

                CurrentNumberLabel.Row(3).Column(6).ColumnSpan(2),

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
            Items.Add(result.FileName);
        }


    }

    private async Task CompleteTask()
    {
        await DisplayAlert("Action", "Completed", "OK");
    }

    // Set the ListView item clicked to StoryNameEntry for editing
    public async Task OnStoryNameItemTapped(string tappedItem)
    {
        if (tappedItem != null)
        {
            StoryNameEntry.Text = tappedItem; // Populate StoryNameEntry with the tapped item text
        }
    }

    // Method to save the edited story name
    public async Task SaveEditedStoryName()
    {
        string editedStoryName = StoryNameEntry.Text;

        // Check if the edited story name is valid (not null or empty)
        if (!string.IsNullOrEmpty(editedStoryName))
        {
            // Check if the current item in the ListView (before editing) exists in the collection
            var currentStoryName = StoryNameEntry.Text;

            // Find the index of the current item in the ObservableCollection (ListView's source)
            int index = Items.IndexOf(currentStoryName);

            if (index >= 0)
            {
                // Update the item in the ObservableCollection with the new edited value
                Items[index] = editedStoryName;  // This will automatically refresh the ListView
            }

            // Optionally, display a success message after saving
            await DisplayAlert("Success", "Story name updated successfully!", "OK");
        }
        else
        {
            // Handle empty input if needed (optional)
            await DisplayAlert("Error", "Please enter a valid story name", "OK");
        }
    }

}
