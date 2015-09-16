using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphaleonis.EventSourceGenerator
{
 /// <summary>
    /// Static class to make creation easier. If possible though, use the extension
    /// method in SmartEnumerableExt.
    /// </summary>
    public static class SmartEnumerableExtensions
    {
        /// <summary>
        /// Extension method to make life easier.
        /// </summary>
        /// <typeparam name="T">Type of enumerable</typeparam>
        /// <param name="source">Source enumerable</param>
        /// <returns>A new SmartEnumerable of the appropriate type</returns>
        public static SmartEnumerable<T> AsSmartEnumerable<T>(this IEnumerable<T> source)
        {
            return new SmartEnumerable<T>(source);
        }
    }

    /// <summary>
    /// Type chaining an IEnumerable&lt;T&gt; to allow the iterating code
    /// to detect the first and last entries simply.
    /// </summary>
    /// <typeparam name="T">Type to iterate over</typeparam>
    public class SmartEnumerable<T> : IEnumerable<SmartEnumerable<T>.Entry>
    {

        /// <summary>
        /// Enumerable we proxy to
        /// </summary>
        readonly IEnumerable<T> m_enumerable;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="enumerable">Collection to enumerate. Must not be null.</param>
        public SmartEnumerable(IEnumerable<T> enumerable)
        {
           if (enumerable == null)
              throw new ArgumentNullException("enumerable", "enumerable is null.");
           
           m_enumerable = enumerable;
        }

        /// <summary>
        /// Returns an enumeration of Entry objects, each of which knows
        /// whether it is the first/last of the enumeration, as well as the
        /// current value and next/previous values.
        /// </summary>
        public IEnumerator<Entry> GetEnumerator()
        {
            using (IEnumerator<T> enumerator = m_enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }
                bool isFirst = true;
                bool isLast = false;
                int index = 0;

                T current = enumerator.Current;
                isLast = !enumerator.MoveNext();
                var entry = new Entry(isFirst, isLast, current, index++);                
                isFirst = false;

                while (!isLast)
                {
                    T next = enumerator.Current;
                    isLast = !enumerator.MoveNext();
                    var entry2 = new Entry(isFirst, isLast, next, index++);                    
                    yield return entry;                   
                    entry = entry2;                    
                }

                yield return entry;
            }
        }

        /// <summary>
        /// Non-generic form of GetEnumerator.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Represents each entry returned within a collection,
        /// containing the value and whether it is the first and/or
        /// the last entry in the collection's. enumeration
        /// </summary>
        public struct Entry
        {
            #region Fields
            
           private readonly bool m_isFirst;
            private readonly bool m_isLast;
            private readonly T m_value;
            private readonly int m_index;

            #endregion

            #region Properties
            /// <summary>
            /// The value of the entry.
            /// </summary>
            public T Value { get { return m_value; } }

            /// <summary>
            /// Whether or not this entry is first in the collection's enumeration.
            /// </summary>
            public bool IsFirst { get { return m_isFirst; } }

            /// <summary>
            /// Whether or not this entry is last in the collection's enumeration.
            /// </summary>
            public bool IsLast { get { return m_isLast; } }

            /// <summary>
            /// The 0-based index of this entry (i.e. how many entries have been returned before this one)
            /// </summary>
            public int Index { get { return m_index; } }
           
            #endregion

            #region Constructors

            internal Entry(bool isFirst, bool isLast, T value, int index)
            {
                m_isFirst = isFirst;
                m_isLast = isLast;
                m_value = value;
                m_index = index;
            }

            #endregion

            #region Methods            

            /// <summary>
            /// Returns "(index)value"
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return String.Format("({0}){1}", Index, Value);
            }

            #endregion

        }
    }
}
