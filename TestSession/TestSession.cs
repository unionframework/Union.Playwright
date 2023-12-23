using System.Collections.Generic;
using Union.Playwright.Core;
using Union.Playwright.Pages;
using Union.Playwright.Services;

namespace Union.Playwright.TestSession
{
    public abstract class MyServicePage: UnionPage
    {
        //[UnionInit("selector", FrameScss="")]
        //public ILocator Button;

        //[InitComponent("selector", FrameScss = "")]
        //public UnionComponent Button;

        //[InitElement("selector", FrameScss = "")]
        //public UnionElement Button;

        //[UnionInit("selector", FrameScss = "")]
        //public UnionElement Button;
    }

    public class MyService: UnionService<MyServicePage>
    {
        public MyService(IServiceContextsPool serviceContextsPool) : base(serviceContextsPool)
        {
        }

        public override string BaseUrl => throw new System.NotImplementedException();
    }

    public abstract class TestSession: ITestSession
    {
        public readonly MyService MyService;

        public TestSession(MyService myService)
        {
            MyService = myService;
        }

        public List<IUnionService> GetServices()
        {
            // TODO: use reflection
            return new List<IUnionService>() { MyService };
        }
    }
}