global using BookApp.Models;
global using BookApp.Pages;

namespace BookApp
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            ConfigV2 configV2 = new ConfigV2();

        }

        private async void OnAudioEpubClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AudioEpubPage());
        }

        private async void OnAudioTextClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AudioTextPage());
        }

        private async void OnLibraryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LibraryPage());
        }

        private async void OnRssFeedClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RssFeedPage());
        }

        private async void OnConfigClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ConfigV2());
        }


    }

}
