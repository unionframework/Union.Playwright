namespace Union.Playwright.Selectors
{
    public class Selector
    {
        public string Xcss { get; set; }
        public string FrameXcss { get; set; }

        //public By By => ScssBuilder.CreateBy(Scss);
        //public By FrameBy => string.IsNullOrWhiteSpace(FrameScss) ? null : ScssBuilder.CreateBy(FrameScss);

        public Selector(string xcss, string frameXcss)
        {
            Xcss = xcss;
            FrameXcss = frameXcss;
        }

        public override string ToString() => $"Xcss: {Xcss}, Frame Xcss: {FrameXcss}";
    }
}