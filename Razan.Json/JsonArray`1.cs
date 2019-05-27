#region License
// Copyright (c) 2019 Ali Mirabdollahi Shams (mirabdollahi.a@gmail.com)
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Razan.Json
{
    // Problem with as read only
    public class JsonArray<T> : IJsonArray<T>
    {
        #region constructors

        public JsonArray(JsonArray source)
        {
            Source = source;
        }
        public JsonArray()
            : this(new JsonArray()) { }

        #endregion

        #region public properties

        public JsonArray Source { get; }

        public bool IsValid => Source.UnderlyingArrayType == null || Source.UnderlyingArrayType == typeof(T);

        public T[] AsArray => Source.AsArray<T>();

        #endregion

        #region IList<T> implementation

        public T this[int index]
        {
            get
            {
                var result = Source[index];

                if (result == null)
                {
                    if (!IsValid)
                    {
                        throw new InvalidCastException($"Underlying array type does not match to '{typeof(T).FullName}'");
                    }

                    return default(T);
                }

                if (result is T variable)
                {
                    return variable;
                }

                if (!IsValid)
                {
                    throw new InvalidCastException($"Underlying array type does not match to '{typeof(T).FullName}'");
                }

                throw new InvalidCastException($"Value '{result}' is not castable to '{typeof(T).FullName}'");
            }
            set => Source[index] = value;
        }

        public int Count => Source.Count;

        public bool IsReadOnly => Source.IsReadOnly;

        public void Add(T item)
        {
            Source.Add(item);
        }

        public void Clear()
        {
            Source.Clear();
        }

        public bool Contains(T item)
        {
            return Source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(array));
            if (Count > array.Length - arrayIndex + 1)
                throw new ArgumentException("The destination array has fewer elements than the collection.");

            for (int i = 0; i < Source.Count; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(Source);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Source.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Source.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return Source.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Source.RemoveAt(index);
        }

        #endregion

        #region public methods

        public void SetContent(IEnumerable<T> collection)
        {
            Source.SetContent(collection.Select(item => (object)item));
        }

        #endregion

        #region inner types

        public class Enumerator : IEnumerator<T>
        {
            public Enumerator(JsonArray source)
            {
                _sourceEnumerator = source.GetEnumerator();
            }

            private IEnumerator _sourceEnumerator;

            public T Current => (T)_sourceEnumerator.Current;

            object IEnumerator.Current => _sourceEnumerator.Current;

            public void Dispose()
            {
                _sourceEnumerator = null;
            }

            public bool MoveNext()
            {
                return _sourceEnumerator.MoveNext();
            }

            public void Reset()
            {
                _sourceEnumerator.Reset();
            }
        }

        #endregion

        public string ToString(EncodingBases encodingModes, bool safeTypeReversible)
        {
            return Source.ToString(encodingModes, safeTypeReversible);
        }

        public string ToString(EncodingBases encodingModes)
        {
            return Source.ToString(encodingModes);
        }
    }
}
