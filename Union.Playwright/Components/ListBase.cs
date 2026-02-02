using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Components
{
    public abstract class ListBase<T> : ComponentBase where T : ItemBase
    {
        protected ListBase(IUnionPage parentPage, string rootScss = null)
            : base(parentPage, rootScss)
        {
        }

        public abstract string ItemIdScss { get; }

        public virtual string IdAttribute => null;

        public async Task<List<string>> GetIdsAsync()
        {
            var locator = this.PlaywrightPage.Locator(this.InnerScss(this.ItemIdScss));
            var count = await locator.CountAsync();
            var ids = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var element = locator.Nth(i);
                string id;
                if (this.IdAttribute == null)
                {
                    id = await element.TextContentAsync();
                }
                else
                {
                    id = await element.GetAttributeAsync(this.IdAttribute);
                }

                if (id != null)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        public async Task<List<T>> GetItemsAsync()
        {
            var ids = await this.GetIdsAsync();
            return ids.Select(id => this.CreateItem(id)).ToList();
        }

        public T CreateItem(string id)
        {
            return (T)Activator.CreateInstance(typeof(T), this, id);
        }

        public async Task<T> FindRandomAsync()
        {
            var ids = await this.GetIdsAsync();
            if (ids.Count == 0)
            {
                return null;
            }

            var random = new Random();
            var id = ids[random.Next(ids.Count)];
            return this.CreateItem(id);
        }

        public async Task<T> FindSingleAsync()
        {
            var ids = await this.GetIdsAsync();
            if (ids.Count == 0)
            {
                return null;
            }

            return this.CreateItem(ids.First());
        }
    }
}
