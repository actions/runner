namespace Microsoft.VisualStudio.Services.Common.Contracts
{
    public class PageHandler
    {
        public string UrlPattern { get; set; }
        public string JavaScript { get; set; }
        public string NavigationJavaScript { get; set; }
        public PageHandlerStyle[] Styles { get; set; }
        public string[] SiteDependencies { get; set; }
        public bool? Terminate { get; set; }
    }
}
