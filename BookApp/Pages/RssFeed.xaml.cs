using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;

namespace BookApp;

public partial class RssFeedPage : ContentPage
{
    private readonly ObservableCollection<RssFeedItem> _feedItems = new();
    private readonly ObservableCollection<string> _rssFeeds = new()
    {
        "https://forums.spacebattles.com/threads/my-next-life-as-a-supervillain-all-routes-lead-to-doctor-doom-hamefura-mcu.1164297/threadmarks.rss?threadmark_category=1",
        "https://forums.sufficientvelocity.com/threads/the-galaxy-is-flood-not-food.123284/threadmarks.rss?threadmark_category=1",
        "https://example.com/rss3"
    };

    public RssFeedPage()
    {
        Title = "RSS Feed";

        var picker = new Picker
        {
            Title = "Select an RSS Feed",
            ItemsSource = _rssFeeds,
            HorizontalOptions = LayoutOptions.FillAndExpand
        };

        picker.SelectedIndexChanged += async (s, e) =>
        {
            if (picker.SelectedIndex >= 0)
            {
                var selectedFeedUrl = _rssFeeds[picker.SelectedIndex];
                await LoadFeed(selectedFeedUrl);
            }
        };

        Content = new StackLayout
        {
            Children =
            {
                new Label
                {
                    Text = "RSS Feed Reader",
                    FontSize = 24,
                    HorizontalTextAlignment = TextAlignment.Center
                }.CenterHorizontal().Margins(0, 10, 0, 10),

                picker.Margins(0, 10, 0, 10),

                new CollectionView
                {
                    ItemsSource = _feedItems,
                    ItemTemplate = new DataTemplate(() =>
                    {
                        var titleLabel = new Label()
                            .FontSize(16)
                            .TextColor(Colors.Black)
                            .Margins(10);

                        titleLabel.SetBinding(Label.TextProperty, nameof(RssFeedItem.Title));

                        var descriptionLabel = new Label()
                            .FontSize(14)
                            .TextColor(Colors.Gray)
                            .Margins(10);

                        descriptionLabel.SetBinding(Label.TextProperty, nameof(RssFeedItem.Description));

                        return new StackLayout
                        {
                            Children = { titleLabel, descriptionLabel }
                        };
                    }),
                    SelectionMode = SelectionMode.Single,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                }
            }
        }.Padding(10);
    }

    private async Task LoadFeed(string rssUrl)
    {
        _feedItems.Clear();

        try
        {
            using var httpClient = new HttpClient();
            var feed = await httpClient.GetStringAsync(rssUrl);

            var rss = XDocument.Parse(feed);
            foreach (var item in rss.Descendants("item"))
            {
                _feedItems.Add(new RssFeedItem
                {
                    Title = item.Element("title")?.Value,
                    Description = item.Element("description")?.Value
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load RSS feed: {ex.Message}", "OK");
        }
    }

    public class RssFeedItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
