using EasyCommands.Utilities.Pika.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyCommands.Utilities {
    public static class Extensions {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            foreach (var item in source)
                action(item);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action) {
            var i = 0;
            foreach (T item in source)
                action(item, i++);
        }

        public static bool MoreThan<T>(this IEnumerable<T> source, int count) => source.Skip(count).Any();

        public static IEnumerable<T> Descent<T>(this T source, Func<T, bool> valid, Func<T, T> next) {
            yield return source;

            var cur = source;
            while (valid(cur))
                yield return cur = next(cur);
        }

        public static IEnumerable<T> Traverse<T>(this T root, Func<T, IEnumerable<T>> collectionSelector)
            => Traverse(new[] { root }, collectionSelector, t => t);
        public static IEnumerable<T> Traverse<T, U>(this T root, Func<T, IEnumerable<U>> collectionSelector, Func<U, T> resultSelector)
            => Traverse(new[] { root }, collectionSelector, resultSelector);

        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> collectionSelector)
            => Traverse(source, collectionSelector, t => t);
        public static IEnumerable<T> Traverse<T, U>(this IEnumerable<T> source, Func<T, IEnumerable<U>> collectionSelector, Func<U, T> resultSelector) {
            var stack = new Stack<IEnumerator<T>>();
            var e = source.GetEnumerator();
            try {
                while (true) {
                    while (e.MoveNext()) {
                        T item = e.Current;
                        yield return item;
                        var elements = collectionSelector(item)?.Select(resultSelector);
                        if (elements == null)
                            continue;
                        stack.Push(e);
                        e = elements.GetEnumerator();
                    }
                    if (stack.Count == 0)
                        break;
                    e.Dispose();
                    e = stack.Pop();
                }
            } finally {
                e.Dispose();
                while (stack.Count != 0)
                    stack.Pop().Dispose();
            }
        }

        public static IntervalUnion ToIntervalUnion<T>(this IEnumerable<T> source, Func<T, int> keySelector, Func<T, int> elementSelector) {
            if (source == null)
                throw new ArgumentNullException("Argument source is null");
            if (keySelector == null)
                throw new ArgumentNullException("Argument keySelector is null");
            if (elementSelector == null)
                throw new ArgumentNullException("Argument elementSelector is null");

            var intervals = new IntervalUnion();
            foreach (T item in source)
                intervals.AddRange(keySelector(item), elementSelector(item));
            return intervals;
        }

        private static Tuple<int, int> GetPossibleIndices<TKey, TValue>(SortedDictionary<TKey, TValue> dict, TKey key, bool strictlyDifferent, out List<TKey> list) {
            list = dict.Keys.ToList();
            int index = list.BinarySearch(key, dict.Comparer);
            if (index >= 0) {
                if (strictlyDifferent)
                    return Tuple.Create(index - 1, index + 1);
                else
                    return Tuple.Create(index, index);
            } else {
                int indexOfBiggerNeighbour = ~index; //bitwise complement of the return value

                if (indexOfBiggerNeighbour == list.Count)
                    return Tuple.Create(list.Count - 1, list.Count);
                else if (indexOfBiggerNeighbour == 0)
                    return Tuple.Create(-1, 0);
                else
                    return Tuple.Create(indexOfBiggerNeighbour - 1, indexOfBiggerNeighbour);
            }
        }

        public static KeyValuePair<TKey, TValue>? Floor<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) {
            var indices = GetPossibleIndices(dictionary, key, false, out List<TKey> list);
            if (indices.Item1 < 0)
                return null;

            var newKey = list[indices.Item1];
            return new KeyValuePair<TKey, TValue>(newKey, dictionary[newKey]);
        }

        public static KeyValuePair<TKey, TValue>? Ceiling<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) {
            var indices = GetPossibleIndices(dictionary, key, false, out List<TKey> list);
            if (indices.Item2 == list.Count)
                return null;

            var newKey = list[indices.Item2];
            return new KeyValuePair<TKey, TValue>(newKey, dictionary[newKey]);
        }

        public static KeyValuePair<TKey, TValue>? LowerEntry<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) {
            var indices = GetPossibleIndices(dictionary, key, true, out List<TKey> list);
            if (indices.Item1 < 0)
                return null;

            var newKey = list[indices.Item1];
            return new KeyValuePair<TKey, TValue>(newKey, dictionary[newKey]);
        }

        public static KeyValuePair<TKey, TValue>? Higher<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key) {
            var indices = GetPossibleIndices(dictionary, key, true, out List<TKey> list);
            if (indices.Item2 == list.Count)
                return null;

            var newKey = list[indices.Item2];
            return new KeyValuePair<TKey, TValue>(newKey, dictionary[newKey]);
        }

        public static TValue GetOrCreateValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> create) {
            if (dictionary.ContainsKey(key))
                return dictionary[key];
            TValue value = create(key);
            if (value == null)
                return default;
            return dictionary[key] = value;

        }
    }
}
