using System.Collections.Generic;
using System.Linq;

namespace RepeatableCallTracer.Common
{
    public static class TracerEqualityHelper
    {
        public static bool SequenceEqual<TSource>(IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first is null)
            {
                return second is null;
            }

            if (second is null)
            {
                return false;
            }

            return Enumerable.SequenceEqual(first, second);
        }
    }
}
