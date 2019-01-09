using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMSProject.Utils
{
    public static class ArrayUtils
    {
        public static void RemovePeakNeighbours(List<double> list, int peakIndex, int range)
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

            for (var i = index; i < index + count; i++)
            {
                list[i] = 0;
            }

            //list.RemoveRange(index, count);
        }
    }
}
