using System;
using System.Collections.Generic;
using System.Reflection;
using Union.Playwright.NUnit.Attributes;
using Union.Playwright.NUnit.Components;
using Union.Playwright.NUnit.Pages.Interfaces;
using Union.Playwright.NUnit.SCSS;

namespace Union.Playwright.NUnit.Pages
{
    public static class WebPageBuilder
    {
        public static void InitPage(UnionPage page)
        {
            InitComponents(page, page);
        }

        /// <summary>
        /// Initializes [UnionInit] fields on a component that was created outside of page activation,
        /// such as items created by ListBase.
        /// </summary>
        public static void InitComponent(IUnionPage page, object component)
        {
            InitComponents(page, component);
        }

        private static void InitComponents(IUnionPage page, object componentContainer)
        {
            var type = componentContainer.GetType();
            var members = GetInitializableMembers(type);

            foreach (var (member, attribute) in members)
            {
                var componentType = GetMemberType(member);
                var component = CreateComponent(page, componentContainer, componentType, attribute);

                if (component is ComponentBase c)
                {
                    c.ComponentName = attribute.ComponentName ?? member.Name;
                    c.FrameScss = attribute.FrameXcss;
                }

                SetMemberValue(componentContainer, member, component);
                InitComponents(page, component);
            }
        }

        public static object CreateComponent(IUnionPage page, object componentContainer, Type type, UnionInit attribute)
        {
            var args = new List<object> { page };

            if (attribute.Args != null && attribute.Args.Length > 0)
            {
                var container = componentContainer as IContainer;
                var processedArgs = new object[attribute.Args.Length];
                Array.Copy(attribute.Args, processedArgs, attribute.Args.Length);

                if (container != null)
                {
                    for (var i = 0; i < processedArgs.Length; i++)
                    {
                        if (processedArgs[i] is string selectorArg)
                        {
                            processedArgs[i] = ScssBuilder.Concat(container.RootScss, selectorArg).Value;
                        }
                    }
                }

                args.AddRange(processedArgs);
            }

            return Activator.CreateInstance(type, args.ToArray());
        }

        internal static T CreateComponent<T>(UnionPage pageBase, object[] args) where T : IComponent
        {
            var allArgs = new List<object> { pageBase };
            if (args != null)
            {
                allArgs.AddRange(args);
            }

            var component = (T)Activator.CreateInstance(typeof(T), allArgs.ToArray());
            InitComponents(pageBase, component);
            return component;
        }

        private static List<(MemberInfo Member, UnionInit Attribute)> GetInitializableMembers(Type type)
        {
            var result = new List<(MemberInfo, UnionInit)>();

            var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<UnionInit>();
                if (attr != null)
                {
                    result.Add((field, attr));
                }
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<UnionInit>();
                if (attr != null)
                {
                    result.Add((prop, attr));
                }
            }

            return result;
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                FieldInfo fi => fi.FieldType,
                PropertyInfo pi => pi.PropertyType,
                _ => throw new NotSupportedException($"Unsupported member type: {member.GetType().Name}")
            };
        }

        private static void SetMemberValue(object target, MemberInfo member, object value)
        {
            switch (member)
            {
                case FieldInfo fi:
                    fi.SetValue(target, value);
                    break;
                case PropertyInfo pi:
                    pi.SetValue(target, value);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member type: {member.GetType().Name}");
            }
        }
    }
}
