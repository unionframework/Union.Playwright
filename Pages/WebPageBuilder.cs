using System;
using Union.Playwright.Pages.Interfaces;

namespace Union.Playwright.Pages
{
    public static class WebPageBuilder
    {
        //public static void InitPage(IPage page)
        //{
        //    InitComponents(page, page);
        //}

        //public static T CreateItem<T>(IContainer container, string id) => (T)CreateComponent<T>(container.ParentPage, container, id);

        //public static List<T> CreateItems<T>(IContainer container, IEnumerable<string> ids)
        //{
        //    var sw = new Stopwatch();
        //    sw.Start();
        //    var items = ids.Select(id => CreateItem<T>(container, id)).ToList();
        //    sw.Stop();
        //    Trace.WriteLine($"Creating {ids.Count()} items of type '{nameof(T)}' took {sw.ElapsedMilliseconds} milliseconds.");
        //    return items;
        //}

        //public static IComponent CreateComponent<T>(IContainer container, params object[] additionalArgs)
        //{
        //    var component = CreateComponent(
        //        container.ParentPage,
        //        container,
        //        typeof(T),
        //        new WebComponentAttribute(additionalArgs),
        //        null);
        //    InitComponents(container.ParentPage, component);
        //    return component;
        //}

        //public static IComponent CreateComponent<T>(IPage page, params object[] additionalArgs)
        //{
        //    var component = CreateComponent(page, page, typeof(T), new WebComponentAttribute(additionalArgs), null);
        //    InitComponents(page, component);
        //    return component;
        //}

        //public static IComponent CreateComponent<T>(
        //    IPage page,
        //    object componentContainer,
        //    params object[] additionalArgs)
        //{
        //    var component = CreateComponent(
        //        page,
        //        componentContainer,
        //        typeof(T),
        //        new WebComponentAttribute(additionalArgs),
        //        null);
        //    InitComponents(page, component);
        //    return component;
        //}

        //public static IComponent CreateComponent(IPage page, Type type, params object[] additionalArgs)
        //{
        //    var component = CreateComponent(page, page, type, new WebComponentAttribute(additionalArgs), null);
        //    InitComponents(page, component);
        //    return component;
        //}

        //public static IComponent CreateComponent(
        //    IPage page,
        //    object componentContainer,
        //    Type type,
        //    IComponentAttribute attribute, string componentFieldName)
        //{
        //    var args = typeof(ItemBase).IsAssignableFrom(type)
        //        ? new List<object> { componentContainer }
        //        : new List<object> { page };
        //    var container = componentContainer as IContainer;
        //    if (attribute.Args != null)
        //    {
        //        if (container != null)
        //        {
        //            for (var i = 0; i < attribute.Args.Length; i++)
        //            {
        //                attribute.Args[i] = CreateInnerSelector(container, attribute.Args[i]);
        //            }
        //        }

        //        args.AddRange(attribute.Args);
        //    }

        //    var componentName = GetComponentName(attribute.ComponentName, componentFieldName, type.Name);
        //    IComponent component;
        //    try
        //    {
        //        component = (IComponent)Activator.CreateInstance(type, args.ToArray());
        //    }
        //    catch (MissingMemberException)
        //    {
        //        Console.WriteLine($"Can not create instance of component '{componentName}' in '{componentContainer.GetType().Name}'.");
        //        throw;
        //    }

        //    component.ComponentName = componentName;
        //    component.FrameScss = attribute.FrameScss ?? container?.FrameScss;
        //    return component;
        //}

        //private static string GetComponentName(string attributeComponentName, string componentFieldName, string componentTypeName)
        //{
        //    if (!string.IsNullOrWhiteSpace(attributeComponentName))
        //    {
        //        return attributeComponentName;
        //    }
        //    if (!string.IsNullOrWhiteSpace(componentFieldName))
        //    {
        //        return componentFieldName.AddSpaces();
        //    }
        //    return componentTypeName;
        //}

        //private static object CreateInnerSelector(IContainer container, object argument)
        //{
        //    var argumentString = argument as string;
        //    if (argumentString != null && argumentString.StartsWith("root:"))
        //    {
        //        return container.InnerScss(argumentString.Replace("root:", string.Empty));
        //    }
        //    return argument;
        //}

        //public static void InitComponents(IPage page, object componentsContainer)
        //{
        //    if (page == null)
        //    {
        //        throw new ArgumentNullException("page", "page cannot be null");
        //    }
        //    if (componentsContainer == null)
        //    {
        //        componentsContainer = page;
        //    }
        //    var type = componentsContainer.GetType();
        //    var components = GetComponents(type);
        //    foreach (var memberInfo in components.Keys)
        //    {
        //        var attribute = components[memberInfo];
        //        IComponent instance;
        //        if (memberInfo is FieldInfo fieldInfo)
        //        {
        //            instance = (IComponent)fieldInfo.GetValue(componentsContainer);
        //            if (instance == null)
        //            {
        //                instance = CreateComponent(page, componentsContainer, fieldInfo.FieldType, attribute, fieldInfo.Name);
        //                fieldInfo.SetValue(componentsContainer, instance);
        //            }
        //            else
        //            {
        //                instance.FrameScss = instance.FrameScss ?? attribute.FrameScss;
        //                instance.ComponentName = attribute.ComponentName ?? instance.ComponentName;
        //            }
        //        }
        //        else if (memberInfo is PropertyInfo propertyInfo)
        //        {
        //            instance = (IComponent)propertyInfo.GetValue(componentsContainer);
        //            if (instance == null)
        //            {
        //                instance = CreateComponent(page, componentsContainer, propertyInfo.PropertyType, attribute, propertyInfo.Name);
        //                propertyInfo.SetValue(componentsContainer, instance);
        //            }
        //            else
        //            {
        //                instance.FrameScss = instance.FrameScss ?? attribute.FrameScss;
        //                instance.ComponentName = attribute.ComponentName ?? instance.ComponentName;
        //            }
        //        }
        //        else
        //        {
        //            throw new NotSupportedException("Unknown member type");
        //        }
        //        page.RegisterComponent(instance);
        //        InitComponents(page, instance);
        //    }
        //}

        //private static Dictionary<MemberInfo, IComponentAttribute> GetComponents(Type type)
        //{
        //    var components = new Dictionary<MemberInfo, IComponentAttribute>();
        //    var members =
        //        type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        //            .Cast<MemberInfo>()
        //            .ToList();
        //    members.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        //    var attributeType = typeof(IComponentAttribute);
        //    foreach (var field in members)
        //    {
        //        var attributes = field.GetCustomAttributes(attributeType, true);
        //        if (attributes.Length == 0)
        //        {
        //            continue;
        //        }
        //        components.Add(field, attributes[0] as IComponentAttribute);
        //    }
        //    return components;
        //}

        //private static bool IsComponent(FieldInfo fieldInfo)
        //{
        //    var type = typeof(IComponent);
        //    return type.IsAssignableFrom(fieldInfo.FieldType);
        //}
        internal static T CreateComponent<T>(UnionPage pageBase, object[] args) where T : IComponent
        {
            throw new NotImplementedException();
        }

        internal static void InitPage(UnionPage unionPage)
        {
            throw new NotImplementedException();
        }
    }
}
