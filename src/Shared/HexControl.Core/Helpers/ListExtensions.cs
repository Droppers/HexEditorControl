using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.Core.Helpers
{
    internal static class ListExtensions
    {
        public static List<T> Clone<T>(this IReadOnlyList<T> list) where T : ICloneable<T>
        {
            var clonedList = new List<T>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                clonedList.Add(list[i].Clone());
            }

            return clonedList;
        }
    }
}
