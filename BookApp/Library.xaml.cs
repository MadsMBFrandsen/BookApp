using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Storage;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VersOne.Epub;

namespace BookApp
{
    public partial class LibraryPage : ContentPage, INotifyPropertyChanged
    {
        private double progress;
        private int totalFiles;
        private int currentFile;

        public double Progress
        {
            get => progress;
            set => SetProperty(ref progress, value);
        }

        public int TotalFiles
        {
            get => totalFiles;
            set => SetProperty(ref totalFiles, value);
        }

        public int CurrentFile
        {
            get => currentFile;
            set => SetProperty(ref currentFile, value);
        }

        public ObservableCollection<LibraryBook> Books { get; set; } = new();

        private readonly string downloadsPath;
        private readonly string libraryFolderPath;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public LibraryPage()
        {
            ConfigPage config = new ConfigPage();

            downloadsPath = Preferences.Get("EpubDefaultPath", "Error");
            //libraryFolderPath = Preferences.Get("LibraryFolderPath","Error");
            //libraryFolderPath = Preferences.Get("LibraryFolderPath", Path.Combine(FileSystem.AppDataDirectory, "MyEpubLibrary"));
            libraryFolderPath = Preferences.Get("LibraryFolderPath", config.defaultLibraryFolderPath);
            Directory.CreateDirectory(libraryFolderPath);

            BindingContext = this;

            Content = new ScrollView
            {
                Padding = 10,
                Content = new StackLayout
                {
                    Children =
            {
                new Frame
                {
                    Padding = 10,
                    BorderColor = Colors.Gray, // Correct color reference
                    BackgroundColor = Colors.Transparent, // Correct color reference
                    Content = new Label { Text = "Library", FontSize = 24, HorizontalOptions = LayoutOptions.Center }
                },

                new Frame
                {
                    Padding = 10,
                    BorderColor = Colors.Gray, // Correct color reference
                    BackgroundColor = Colors.Transparent, // Correct color reference
                    Content = new ProgressBar().Bind(ProgressBar.ProgressProperty, nameof(Progress), BindingMode.OneWay)
                },

                new Frame
                {
                    Padding = 10,
                    BorderColor = Colors.Gray, // Correct color reference
                    BackgroundColor = Colors.Transparent, // Correct color reference
                    Content = new Button { Text = "Load EPUBs" }
                        .BindCommand(nameof(LoadEpubsCommand), source: this)
                },

                new Frame
                {
                    Padding = 10,
                    BorderColor = Colors.Gray, // Correct color reference
                    BackgroundColor = Colors.Transparent, // Correct color reference
                    Content = new Button { Text = "Load Library" }
                        .BindCommand(nameof(LoadLibraryCommand), source: this)
                },

                new Frame
                {
                    Padding = 10,
                    BorderColor = Colors.Gray, // Correct color reference
                    BackgroundColor = Colors.Transparent, // Correct color reference
                    Content = new CollectionView
{
    ItemsSource = Books,
    ItemTemplate = new DataTemplate(() =>
    {
        var titleLabel = new Label { FontSize = 16 }.Bind(Label.TextProperty, "Title");
        var firstChapterLabel = new Label { FontSize = 12 }.Bind(Label.TextProperty, "FirstChapter");
        var lastChapterLabel = new Label { FontSize = 12 }.Bind(Label.TextProperty, "LastChapter");
        var chapterCountLabel = new Label { FontSize = 12 }.Bind(Label.TextProperty, "ChaptersCount");
        var actionButton = new Button { Text = "Details" };
        actionButton.Clicked += OnActionButtonClicked;

        // Create a Frame to wrap the Grid and add borders
        var frameWithBorder = new Frame
        {
            Padding = 10, // Padding inside the Frame
            BorderColor = Colors.Gray, // Border color for the Frame
            BackgroundColor = Colors.Transparent, // Transparent background so grid content is visible
            CornerRadius = 10, // Optional: Rounded corners for the Frame
            Content = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    titleLabel.Row(0).Column(0),
                    firstChapterLabel.Row(0).Column(1),
                    lastChapterLabel.Row(0).Column(2),
                    chapterCountLabel.Row(0).Column(3),
                    actionButton.Row(0).Column(4)
                }
            }
        };

        return frameWithBorder; // Return the Frame containing the Grid
    })
}

                }
            }
                }
            };
        }

        public Command LoadEpubsCommand => new(async () => await LoadEpubsAsync());
        public Command LoadLibraryCommand => new(LoadLibrary);

        private async Task LoadEpubsAsync()
        {
            Progress = 0;
            CurrentFile = 0;

            var epubFiles = Directory.GetFiles(downloadsPath, "*.epub", SearchOption.AllDirectories);
            TotalFiles = epubFiles.Length;

            foreach (var file in epubFiles)
            {
                try
                {
                    var epubBook = await Task.Run(() => EpubReader.ReadBook(file));
                    ProcessBook(epubBook);

                    CurrentFile++;
                    Progress = (double)CurrentFile / TotalFiles;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to process {file}: {ex.Message}", "OK");
                }
            }

            await DisplayAlert("Done", "EPUBs loaded successfully!", "OK");
        }

        private void LoadLibrary()
        {
            Books.Clear();

            if (Directory.Exists(libraryFolderPath))
            {
                foreach (var bookDir in Directory.GetDirectories(libraryFolderPath))
                {
                    var libraryBook = new LibraryBook
                    {
                        Title = Path.GetFileName(bookDir)
                    };

                    var chapterFiles = Directory.GetFiles(bookDir, "*.txt").OrderBy(f => f).ToList();
                    foreach (var chapterFile in chapterFiles)
                    {
                        var content = File.ReadAllText(chapterFile);
                        var chapterTitle = Path.GetFileNameWithoutExtension(chapterFile);

                        var chapter = new Chapter
                        {
                            Filepath = chapterFile,
                            Title = chapterTitle,
                            Content = content
                        };

                        libraryBook.Chapters.Add(chapter);
                    }

                    // Set the first and last chapter titles
                    if (libraryBook.Chapters.Any())
                    {
                        libraryBook.FirstChapter = libraryBook.Chapters.First().Title;
                        libraryBook.LastChapter = libraryBook.Chapters.Last().Title;
                    }

                    Books.Add(libraryBook);
                }
            }
        }



        private void ProcessBook(EpubBook epubBook)
        {
            string cleanedTitle = Regex.Replace(epubBook.Title, @"[^A-Za-z0-9\s]", "").Trim();
            cleanedTitle = Regex.Replace(cleanedTitle, @"\s+", " ");

            var bookFolderPath = Path.Combine(libraryFolderPath, cleanedTitle);
            Directory.CreateDirectory(bookFolderPath);

            foreach (var chapter in epubBook.ReadingOrder)
            {
                var chapterTitle = Path.GetFileNameWithoutExtension(chapter.FilePath);
                var outputPath = Path.Combine(bookFolderPath, $"{chapterTitle}.txt");

                if (!File.Exists(outputPath))
                {
                    var content = EditContent(chapter.Content);
                    File.WriteAllText(outputPath, content);
                }

                var chapterObj = new Chapter
                {
                    Filepath = outputPath,
                    Title = chapterTitle,
                    Content = File.ReadAllText(outputPath)
                };

                var book = Books.FirstOrDefault(b => b.Title == cleanedTitle);
                if (book == null)
                {
                    book = new LibraryBook { Title = cleanedTitle };
                    Books.Add(book);
                }

                book.Chapters.Add(chapterObj);
            }
        }

        private static string EditContent(string content)
        {
            var cleanText = Regex.Replace(content, "<.*?>", string.Empty);
            cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();
            return cleanText;
        }

        private void OnActionButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is LibraryBook book)
            {
                DisplayAlert("Book Details", $"Title: {book.Title}\nChapters: {book.Chapters.Count}", "OK");
            }
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class LibraryBook
        {
            public string Title { get; set; }
            public ObservableCollection<Chapter> Chapters { get; set; } = new();
            public string FirstChapter { get; set; }
            public string LastChapter { get; set; }
            public int ChaptersCount => Chapters.Count;
        }
    }
}
