using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMSProject.Utils
{
    public static class ArrayUtils<T>
    {
        public static void RemovePeakNeighbours(List<T> list, int peakIndex, int range)
        {
            var index = peakIndex - range;
            if (index < 0)
            {
                index = 0;
            }

            var count = range * 2 + 1;

            if ((index + count) > list.Count)
            {
                count = list.Count - index;
            }

            list.RemoveRange(index, count);
        }
    }
}
