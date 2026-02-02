using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Components
{
    public abstract class ItemBase : ComponentBase
    {
        private string _itemRootScss;

        public string Id { get; }

        protected ItemBase(IContainer container, string id)
            : base(container.ParentPage)
        {
            this.Id = id;
        }

        public abstract string ItemScss { get; }

        public override string RootScss
        {
            get
            {
                if (this._itemRootScss == null)
                {
                    this._itemRootScss = this.ItemScss;
                }

                return this._itemRootScss;
            }
        }
    }
}
