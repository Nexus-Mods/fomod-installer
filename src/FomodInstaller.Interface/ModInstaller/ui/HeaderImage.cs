namespace FomodInstaller.Interface
{
    public class HeaderImage
    {
        public string path { get; set; }
        public bool showFade { get; set; }
        public int height { get; set; }

        public HeaderImage(string path, bool showFade, int height)
        {
            this.path = path;
            this.showFade = showFade;
            this.height = height;
        }
    }
}