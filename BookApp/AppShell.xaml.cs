using BookApp.Pages;

namespace BookApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for your pages
            Routing.RegisterRoute("AudioEpubPage", typeof(AudioEpubPage));
            Routing.RegisterRoute("AudioTextPage", typeof(AudioTextPage));
            Routing.RegisterRoute("LibraryPage", typeof(LibraryPage));
            Routing.RegisterRoute("RssFeedPage", typeof(RssFeedPage));
            Routing.RegisterRoute("ConfigV2", typeof(ConfigV2));
            //Routing.RegisterRoute("ConfigPage", typeof(ConfigPage));
        }
    }
}
