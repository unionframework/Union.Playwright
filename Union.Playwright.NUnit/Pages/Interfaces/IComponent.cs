using Microsoft.Playwright;

namespace Union.Playwright.NUnit.Pages.Interfaces
{
    public interface IComponent
    {
        IPage ParentPage { get; }

        string ComponentName { get; set; }

        string FrameScss { get; set; }

        bool IsVisible();
    }
}