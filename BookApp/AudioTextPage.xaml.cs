using BookApp.Fungtions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xceed.Words.NET;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp
{
    public partial class AudioTextPage : ContentPage
    {
        private Entry chapterTitleEntry, storyNameEntry;
        private Editor chapterTextEditor, contentEditor;  // Define contentEditor here
        private Label timeLeftLabel;
        private List<Chapter> chapters = new List<Chapter>();
        private Epub epub = new();
        private Button CompletedButton;
        private Button SelectTextFolderButton;
        private Button GetStoryNameButton;
        private Button GetTxtFileButton;
        private Label storyNameLabel, titleLabel, timeLeftLabelDisplay, chapterTitle;
        private Entry storyNameEntryField, chapterTitleEntryField;
        private Button ClearButton;

        public AudioTextPage()
        {
            // Initialize the SelectTextFolderButton
            SelectTextFolderButton = new Button { Text = "Folder", BackgroundColor = Colors.Blue, TextColor = Colors.White };
            SelectTextFolderButton.Clicked += async (s, e) => await FolderClicked();
            SelectTextFolderButton.Row(1).Column(6);

            // Initialize the GetTxtFileButton
            GetTxtFileButton = new Button { Text = "Txt File" };
            GetTxtFileButton.Clicked += async (s, e) => await OnGetTxtFileClicked();
            GetTxtFileButton.Row(1).Column(5);

            // Initialize the GetStoryNameButton
            GetStoryNameButton = new Button { Text = "Get Story Name" };
            GetStoryNameButton.Clicked += async (s, e) => await OnGetStoryNameButtonClicked();
            GetStoryNameButton.Row(3).Column(7);

            // Initialize the CompletedButton
            CompletedButton = new Button { Text = "Create Sound" };
            CompletedButton.Clicked += async (s, e) => await OnCreateButtonClicked();
            CompletedButton.Row(7).Column(7);

            // Initialize the Clear Button
            ClearButton = new Button { Text = "Clear", BackgroundColor = Colors.Red, TextColor = Colors.White };
            ClearButton.Clicked += OnClearButtonClicked;
            ClearButton.Row(8).Column(7); // Set its position in the grid

            // Initialize the Story Name Label
            storyNameLabel = new Label { Text = "Story Name" };
            storyNameLabel.Row(4).Column(5);

            // Initialize the Story Name Label
            chapterTitle = new Label { Text = "Chapter Title" };
            chapterTitle.Row(7).Column(5).ColumnSpan(2);

            // Initialize the Story Name Entry Field
            storyNameEntryField = new Entry { Placeholder = "Enter story name" };
            storyNameEntryField.Row(4).Column(6).ColumnSpan(2);
            storyNameEntryField.Assign(out storyNameEntry);

            // Initialize the Title Label
            titleLabel = new Label { Text = "Title" };
            titleLabel.Row(6).Column(5);

            // Initialize the Chapter Title Entry Field
            chapterTitleEntryField = new Entry { Placeholder = "Enter chapter title" };
            chapterTitleEntryField.Row(6).Column(6).ColumnSpan(2);
            chapterTitleEntryField.Assign(out chapterTitleEntry);

            // Initialize the Time Left Label
            timeLeftLabelDisplay = new Label { Text = "Time Left" };
            timeLeftLabelDisplay.Row(2).Column(6);

            // Initialize the Time Left Display
            timeLeftLabel = new Label { Text = "Na" };
            timeLeftLabel.Row(2).Column(7).ColumnSpan(2);
            timeLeftLabel.Assign(out timeLeftLabel);

            // Initialize the Chapter Text Editor
            chapterTextEditor = new Editor
            {
                Placeholder = "Enter chapter text here...",
                AutoSize = EditorAutoSizeOption.TextChanges
            };
            //chapterTextEditor.RowSpan(10).ColumnSpan(5);

            // Initialize the Content Editor for displaying chapter content
            contentEditor = new Editor
            {
                Placeholder = "Chapter content will be displayed here",
                AutoSize = EditorAutoSizeOption.TextChanges
            };
            contentEditor.Row(10).Column(0).ColumnSpan(5);

            // Wrap the Editor in a Frame to clip overflow
            var clippedEditorFrame = new Frame
            {
                Content = chapterTextEditor,
                HeightRequest = 250, // Set a height limit to control the visible space
                BorderColor = Colors.Black,
                BackgroundColor = Colors.White,
                CornerRadius = 5,
                Padding = 5,
                IsClippedToBounds = true, // This clips any overflow content
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.Start
            };
            clippedEditorFrame.RowSpan(10).ColumnSpan(5);

            // Set up the grid layout
            Content = new Grid
            {
                RowDefinitions = Rows.Define(Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto, Star),
                ColumnDefinitions = Columns.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),
                Children =
                {
                    //chapterTextEditor,
                    clippedEditorFrame,
                    GetTxtFileButton,
                    SelectTextFolderButton,
                    storyNameLabel,
                    storyNameEntryField,
                    GetStoryNameButton,
                    titleLabel,
                    chapterTitleEntryField,
                    timeLeftLabelDisplay,
                    timeLeftLabel,
                    contentEditor,
                    chapterTitle,
                    ClearButton,
                    CompletedButton
                }
            }.FillHorizontal().FillVertical();
        }

        private async Task OnCreateButtonClicked()
        {
            ConvertTextToSound textToSound = new();
            try
            {
                epub.Title = storyNameEntryField.Text.Trim();

                // Retrieve the sound files path from Preferences
                string soundfilespath = Preferences.Get("SoundFilesPath", "Error");

                // Check if the sound files path is valid
                if (soundfilespath == "Error" || !Directory.Exists(soundfilespath))
                {
                    await DisplayAlert("Error", "Invalid or missing sound files path.", "OK");
                    return;
                }

                const double timePerWordInSeconds = 0.004;
                int totalWordsAllChapters = epub.Chapters.Sum(Ch => Ch.WordCount);
                double totalTimeLeft = totalWordsAllChapters * timePerWordInSeconds;

                timeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";

                foreach (Chapter item in epub.Chapters)
                {
                    chapterTitle.Text = item.Title;
                    double chapterTime = item.WordCount * timePerWordInSeconds; // Time for this chapter
                    totalTimeLeft -= chapterTime;

                    if (epub.Chapters.Count > 1)
                    {
                        // Update the remaining time across all EPUBs
                        timeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";
                    }

                    await textToSound.CreateSoundFileAsync(item, epub.Title, soundfilespath);

                    if (epub.Chapters.Count == 1)
                    {
                        // Update the remaining time across all EPUBs
                        timeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";
                    }
                }
            }
            catch (Exception ex)
            {
                // Display any errors that occur during the process
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task OnGetTxtFileClicked()
        {
            try
            {
                // Use FilePicker to select a single file (txt or docx)
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>> {
                        { DevicePlatform.iOS, new[] { "public.text" } },
                        { DevicePlatform.Android, new[] { ".txt", ".docx" } },
                        { DevicePlatform.WinUI, new[] { ".txt", ".docx" } },
                    })
                });

                if (result == null)
                {
                    // User canceled file selection
                    return;
                }

                // Get the file path
                string selectedFilePath = result.FullPath;

                var chapter = new Chapter
                {
                    Title = Path.GetFileNameWithoutExtension(selectedFilePath)  // Use file name without extension as title
                };

                if (selectedFilePath.EndsWith(".txt"))
                {
                    chapter.Content = await File.ReadAllTextAsync(selectedFilePath);
                }
                else if (selectedFilePath.EndsWith(".docx"))
                {
                    chapter.Content = ReadDocxFile(selectedFilePath);
                }

                epub.Chapters.Clear();  // Clear any previous chapters

                epub.Chapters.Add(chapter);  // Add the single chapter

                // Display file title in the Editor
                chapterTextEditor.Text = chapter.Title;
                //
                string storyname = Path.GetFileName(selectedFilePath);
                storyname.Replace(".docx", "");
                storyname.Replace(".txt", "");
                storyNameEntryField.Text = storyname;

                // Optionally, display content of the chapter
                contentEditor.Text = chapter.Content;

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task FolderClicked()
        {
            try
            {
                // Use FolderPicker to select a folder
                var result = await FolderPicker.PickAsync(default);

                if (result == null)
                {
                    // User canceled folder selection
                    return;
                }

                string selectedFolderPath = result.Folder.Path;

                // Find all .txt and .docx files in the folder
                var txtFiles = Directory.GetFiles(selectedFolderPath, "*.txt");
                var docxFiles = Directory.GetFiles(selectedFolderPath, "*.docx");
                var allFiles = txtFiles.Concat(docxFiles);

                foreach (var filePath in allFiles)
                {
                    var chapter = new Chapter
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath)
                    };

                    if (filePath.EndsWith(".txt"))
                    {
                        chapter.Content = await File.ReadAllTextAsync(filePath);
                    }
                    else if (filePath.EndsWith(".docx"))
                    {
                        chapter.Content = ReadDocxFile(filePath);
                    }
                    storyNameEntryField.Text = Path.GetFileName(selectedFolderPath);
                    //epub.Title = Path.GetFileName(selectedFolderPath);
                    epub.Chapters.Add(chapter);
                }

                // Update the Editor with the chapter titles
                chapterTextEditor.Text = string.Join(Environment.NewLine, epub.Chapters.Select(ch => ch.Title));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task OnGetStoryNameButtonClicked()
        {
            // Use FolderPicker to select a folder
            var result = await FolderPicker.PickAsync(default);

            if (result == null)
            {
                // User canceled folder selection
                return;
            }

            storyNameEntryField.Text = Path.GetFileName(result.Folder.Path);
        }

        private string ReadDocxFile(string filePath)
        {
            using var document = DocX.Load(filePath);
            return document.Text;
        }

        private void OnClearButtonClicked(object sender, EventArgs e)
        {
            // Reset Entry fields
            storyNameEntryField.Text = string.Empty;
            chapterTitleEntryField.Text = string.Empty;

            // Reset Editors
            chapterTextEditor.Text = string.Empty;
            contentEditor.Text = string.Empty;

            // Reset Labels
            timeLeftLabel.Text = "Na";
            chapterTitle.Text = "Chapter Title";

            // Clear the epub object
            epub.Chapters.Clear();
        }
    }
}
