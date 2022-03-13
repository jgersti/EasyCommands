using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities.Pika.Utils {
    public class IntervalUnion {
        public SortedDictionary<int, int> NonOverlappingRanges { get; } = new SortedDictionary<int, int>();

        public void AddRange(int start, int end) {
            if (start > end)
                throw new ArgumentException("Invalid Interval");

            int newStart, newEnd;

            var floor = NonOverlappingRanges.Floor(start);
            if ((floor?.Value ?? -1) < start) {
                newStart = start;
                newEnd = end;
            } else {
                newStart = floor.Value.Key;
                newEnd = Math.Max(floor.Value.Value, end);
            }

            var higher = NonOverlappingRanges.Higher(newStart);
            if (higher != null && higher?.Key <= newEnd) {
                NonOverlappingRanges.Remove(higher.Value.Key);
                NonOverlappingRanges[newStart] = Math.Max(newEnd, higher.Value.Value);
            } else
                NonOverlappingRanges[newStart] = newEnd;
        }

        public IntervalUnion Invert(int start, int end) {
            var inverted = new IntervalUnion();
            int prev = start;
            if (NonOverlappingRanges.Count > 0) {
                foreach(var kv in NonOverlappingRanges.TakeWhile(kv => kv.Key > end)) {
                    if (kv.Key > prev)
                        inverted.AddRange(prev, kv.Key);
                    prev = kv.Value;
                }
            } else
                inverted.AddRange(start, end);
            return inverted;
        }

        public bool RangeOverlaps(int start, int end) {
            var floor = NonOverlappingRanges.Floor(start);
            if (floor != null) {
                var  floorStart = floor.Value.Key;
                var floorEnd = floor.Value.Value;
                if (Math.Max(end, floorEnd) - Math.Min(start, floorStart) < (end - start) + (floorEnd - floorStart))
                    return true;
            }

            var ceil = NonOverlappingRanges.Ceiling(start);
            if (ceil != null) {
                var  ceilStart = ceil.Value.Key;
                var ceilEnd = ceil.Value.Value;
                if (Math.Max(end, ceilEnd) - Math.Min(start, ceilStart) < (end - start) + (ceilEnd - ceilStart))
                    return true;
            }

            return false;
        }
    }
}
