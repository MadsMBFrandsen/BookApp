using BookApp.Fungtions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Xceed.Words.NET;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace BookApp
{
    public partial class AudioTextPage : ContentPage
    {
        private Entry chapterTitleEntry, storyNameEntry;
        private Editor chapterTextEditor, contentEditor;
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
            GetTxtFileButton = new Button { Text = "Txt / Docx / Pdf" };
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
            ClearButton.Row(8).Column(7);

            // Initialize the Story Name Label
            storyNameLabel = new Label { Text = "Story Name" };
            storyNameLabel.Row(4).Column(5);

            // Initialize the Chapter Title Label
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

            // Primary editor (top-left area)
            chapterTextEditor = new Editor
            {
                Placeholder = "Enter chapter text here...",
                AutoSize = EditorAutoSizeOption.TextChanges
            };

            // Viewer/editor for loaded chapter content (bottom-left row; will be wrapped in a ScrollView)
            contentEditor = new Editor
            {
                Placeholder = "Chapter content will be displayed here",
                AutoSize = EditorAutoSizeOption.TextChanges,
                IsReadOnly = false,                 // set true if you want view-only
                Margin = new Thickness(6)           // readability
            };

            // Make the bottom row scrollable
            var scrollableContentEditor = new ScrollView
            {
                Content = contentEditor,
                Orientation = ScrollOrientation.Vertical
            };
            // Place it in the last (Star) row, columns 0–4
            scrollableContentEditor.Row(9).Column(0).ColumnSpan(5);

            // Wrap the top editor in a Frame to clip overflow
            var clippedEditorFrame = new Frame
            {
                Content = chapterTextEditor,
                HeightRequest = 250,
                BorderColor = Colors.Black,
                BackgroundColor = Colors.White,
                CornerRadius = 5,
                Padding = 5,
                IsClippedToBounds = true,
                HorizontalOptions = LayoutOptions.FillAndExpand,
                VerticalOptions = LayoutOptions.Start
            };
            // IMPORTANT: Span rows 0–8 so row 9 stays free for the scrollable viewer
            clippedEditorFrame.RowSpan(9).ColumnSpan(5);

            // Grid layout (10 rows: indices 0..9)
            Content = new Grid
            {
                RowDefinitions = Rows.Define(Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto, Auto, Star),
                ColumnDefinitions = Columns.Define(Star, Star, Star, Star, Star, Star, Star, Star, Star, Star),
                Children =
                {
                    clippedEditorFrame,
                    scrollableContentEditor,      // << replaces direct contentEditor placement
                    GetTxtFileButton,
                    SelectTextFolderButton,
                    storyNameLabel,
                    storyNameEntryField,
                    GetStoryNameButton,
                    titleLabel,
                    chapterTitleEntryField,
                    timeLeftLabelDisplay,
                    timeLeftLabel,
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
                epub.Title = storyNameEntryField.Text?.Trim();

                // Retrieve the sound files path from Preferences
                string soundfilespath = Preferences.Get("SoundFilesPath", "Error");

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
                    double chapterTime = item.WordCount * timePerWordInSeconds;
                    totalTimeLeft -= chapterTime;

                    if (epub.Chapters.Count > 1)
                    {
                        timeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";
                    }

                    await textToSound.CreateSoundFileAsync(item, epub.Title, soundfilespath);

                    if (epub.Chapters.Count == 1)
                    {
                        timeLeftLabel.Text = $"Time Left: {TimeSpan.FromSeconds(totalTimeLeft):hh\\:mm\\:ss}";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task OnGetTxtFileClicked()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        // iOS: UTType identifiers
                        { DevicePlatform.iOS, new[] { "public.plain-text", "org.openxmlformats.wordprocessingml.document", "com.adobe.pdf" } },
                        // Android: MIME types (extensions also work, but MIME is most reliable)
                        { DevicePlatform.Android, new[] { "text/plain", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/pdf" } },
                        // Windows: file extensions
                        { DevicePlatform.WinUI, new[] { ".txt", ".docx", ".pdf" } },
                    })
                });

                if (result == null) return;

                string selectedFilePath = result.FullPath;

                var chapter = new Chapter
                {
                    Title = Path.GetFileNameWithoutExtension(selectedFilePath)
                };

                if (selectedFilePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    chapter.Content = await File.ReadAllTextAsync(selectedFilePath);
                }
                else if (selectedFilePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    chapter.Content = ReadDocxFile(selectedFilePath);
                }
                else if (selectedFilePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    chapter.Content = ReadPdfFile(selectedFilePath);
                }

                epub.Chapters.Clear();
                epub.Chapters.Add(chapter);

                // Display file title in the Editor
                chapterTextEditor.Text = chapter.Title;

                // Correctly set the story name without extension
                storyNameEntryField.Text = Path.GetFileNameWithoutExtension(selectedFilePath);

                // Display content
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
                var result = await FolderPicker.PickAsync(default);
                if (result == null) return;

                string selectedFolderPath = result.Folder.Path;

                var txtFiles = Directory.GetFiles(selectedFolderPath, "*.txt");
                var docxFiles = Directory.GetFiles(selectedFolderPath, "*.docx");
                var pdfFiles = Directory.GetFiles(selectedFolderPath, "*.pdf");

                var allFiles = txtFiles.Concat(docxFiles).Concat(pdfFiles);

                epub.Chapters.Clear();

                foreach (var filePath in allFiles)
                {
                    var chapter = new Chapter
                    {
                        Title = Path.GetFileNameWithoutExtension(filePath)
                    };

                    if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        chapter.Content = await File.ReadAllTextAsync(filePath);
                    }
                    else if (filePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    {
                        chapter.Content = ReadDocxFile(filePath);
                    }
                    else if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        chapter.Content = ReadPdfFile(filePath);
                    }

                    epub.Chapters.Add(chapter);
                }

                storyNameEntryField.Text = Path.GetFileName(selectedFolderPath);
                chapterTextEditor.Text = string.Join(Environment.NewLine, epub.Chapters.Select(ch => ch.Title));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private async Task OnGetStoryNameButtonClicked()
        {
            var result = await FolderPicker.PickAsync(default);
            if (result == null) return;

            storyNameEntryField.Text = Path.GetFileName(result.Folder.Path);
        }

        private string ReadDocxFile(string filePath)
        {
            using var document = DocX.Load(filePath);
            return document.Text;
        }

        private string ReadPdfFile(string filePath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                foreach (var page in document.GetPages())
                {
                    // PdfPig provides a reasonable linearized text output
                    sb.AppendLine(page.Text);
                }
            }

            return sb.ToString();
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
