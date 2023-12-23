using System;

namespace Union.Playwright.Attributes
{
    public class UnionInit : Attribute
    {
        public UnionInit(params object[] args)
        {
            Args = args;
        }

        public object[] Args { get; }

        public string ComponentName { get; set; }

        public string FrameXcss { get; set; }
    }
}