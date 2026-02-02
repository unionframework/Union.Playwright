using Union.Playwright.NUnit.Pages.Interfaces;

namespace Union.Playwright.NUnit.Components
{
    public abstract class ItemBase : ComponentBase
    {
        private string _itemRootScss;

        protected IContainer Container { get; }

        public string Id { get; }

        protected ItemBase(IContainer container, string id)
            : base(container.ParentPage)
        {
            this.Container = container;
            this.Id = id;
        }

        public abstract string ItemScss { get; }

        public override string RootScss
        {
            get
            {
                if (_itemRootScss == null)
                {
                    _itemRootScss = this.ItemScss;
                }

                return _itemRootScss;
            }
        }
    }
}
