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

namespace Razan.Json
{
    public class ReadOnlyJsonArray : IReadOnlyList<object>
    {
        #region constructors

        public ReadOnlyJsonArray(JsonArray source)
        {
            _source = source;
        }

        public ReadOnlyJsonArray()
            : this(new JsonArray())
        {
            
        }

        #endregion

        #region private fields

        private readonly JsonArray _source;

        #endregion

        #region private methods

        private object getReadOnlyIfAvailable(object obj)
        {
            var jsonArray = obj as JsonArray;
            if (jsonArray != null) { return jsonArray.AsReadOnly; }
            var jsonObject = obj as JsonObject;
            if (jsonObject != null) { return jsonObject.AsReadOnly; }

            return obj;
        }

        #endregion

        #region IReadOnlyList<object> implementation

        public object this[int index]
        {
            get
            {
                var result = _source[index];

                return getReadOnlyIfAvailable(result);
            }
        }

        public int Count => _source.Count;

        public IEnumerator<object> GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _source.GetEnumerator();
        }

        #endregion

        #region methods

        public T[] AsArray<T>()
        {
            return _source.AsArray<T>();
        }

        #endregion

        #region override methods

        public override string ToString()
        {
            return _source.ToString();
        }

        #endregion
    }
}
