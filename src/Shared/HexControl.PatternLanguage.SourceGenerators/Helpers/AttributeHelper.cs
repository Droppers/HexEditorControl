using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.SourceGenerators.Helpers
{
    internal static class AttributeHelper
    {
        public static IReadOnlyList<(Type type, TAttribute attribute)> GetTypesWithAttribute<TAttribute>() where TAttribute : Attribute
        {
            return
                (from a in AppDomain.CurrentDomain.GetAssemblies()
                from type in a.GetTypes()
                let attributes = type.GetCustomAttributes(typeof(TAttribute), true)
                where attributes.Length > 0
                select (type, (TAttribute)attributes.First())).ToList();
        }
    }
}
