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
using System.Dynamic;
using System.Linq;
using static Razan.Json.JsonObject;

namespace Razan.Json
{
    public class ReadOnlyJsonObject : DynamicObject, IReadOnlyDictionary<string, object>
    {
        #region constructors

        public ReadOnlyJsonObject(JsonObject source)
        {
            _storage = source;
        }

        #endregion

        #region private fields

        private readonly JsonObject _storage;

        #endregion

        #region properties

        public dynamic AsDynamic => this;

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

        #region IReadOnlyDictionary<string, object> implementation

        public IEnumerable<string> Keys => _storage.Keys;

        public IEnumerable<object> Values => _storage.Values.Select(getReadOnlyIfAvailable);

        public int Count => _storage.Count;

        public object this[string key]
        {
            get
            {
                var result = _storage[key];

                return getReadOnlyIfAvailable(result);
            }
        }

        public bool ContainsKey(string key)
        {
            return _storage.ContainsKey(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            object value2;
            var result = _storage.TryGetValue(key, out value2);

            value = result ? getReadOnlyIfAvailable(value2) : null;

            return result;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        #endregion

        #region DynamicObject overrides

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _storage.Keys;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var result2 = _storage.TryGetValue(binder.Name, out result);

            if (result is JsonObject) { result = ((JsonObject)result).AsReadOnly; }
            else if (result is JsonArray) { result = ((JsonArray)result).AsReadOnly; }

            return result2;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            throw new InvalidOperationException("Current instance is Read-Only!");
        }

        #endregion

        #region indexers

        public bool this[string key, bool fallback] => _storage[key, fallback];

        public byte this[string key, byte fallback] => _storage[key, fallback];

        public int this[string key, int fallback] => _storage[key, fallback];

        public long this[string key, long fallback] => _storage[key, fallback];

        public decimal this[string key, decimal fallback] => _storage[key, fallback];

        public float this[string key, float fallback] => _storage[key, fallback];

        public double this[string key, double fallback] => _storage[key, fallback];

        public string this[string key, string fallback] => _storage[key, fallback];

        public DateTime this[string key, DateTime fallback] => _storage[key, fallback];

        public ReadOnlyJsonObject this[string key, ReadOnlyJsonObject fallback] => _storage[key, fallback];

        public ReadOnlyJsonArray this[string key, ReadOnlyJsonArray fallback] => _storage[key, fallback];

        #endregion

        #region override methods

        public override string ToString()
        {
            return _storage.ToString();
        }

        #endregion

        #region public methods

        public string ToString(EncodingBases encodingModes, bool safeTypeReversible)
        {
            return _storage.ToString(encodingModes, safeTypeReversible);
        }
        public string ToString(EncodingBases encodingModes)
        {
            return _storage.ToString(encodingModes);
        }
        public string ToString(bool safeTypeReversible)
        {
            return _storage.ToString(safeTypeReversible);
        }

        public void WriteTo(string fileName, EncodingBases encodingModes, bool safeTypeReversible)
        {
            _storage.WriteTo(fileName, encodingModes, safeTypeReversible);
        }
        public void WriteTo(string fileName, EncodingBases encodingModes)
        {
            _storage.WriteTo(fileName, encodingModes);
        }
        public void WriteTo(string fileName, bool safeTypeReversible)
        {
            _storage.WriteTo(fileName, safeTypeReversible);
        }
        public void WriteTo(string fileName)
        {
            _storage.WriteTo(fileName);
        }

        public object Get(string key)
        {
            return _storage.Get(key);
        }

        public TType Get<TType>(string key)
        {
            return _storage.Get<TType>(key);
        }

        public bool GetBoolean(string key)
        {
            return _storage.GetBoolean(key);
        }

        public byte GetInt16(string key)
        {
            return _storage.GetInt16(key);
        }

        public int GetInt32(string key)
        {
            return _storage.GetInt32(key);
        }

        public long GetInt64(string key)
        {
            return _storage.GetInt64(key);
        }

        public decimal GetDecimal(string key)
        {
            return _storage.GetDecimal(key);
        }

        public float GetSingle(string key)
        {
            return _storage.GetSingle(key);
        }

        public double GetDouble(string key)
        {
            return _storage.GetDouble(key);
        }

        public string GetString(string key)
        {
            return _storage.GetString(key);
        }

        public DateTime GetDateTime(string key)
        {
            return _storage.GetDateTime(key);
        }

        public ReadOnlyJsonObject GetReadOnlyJsonObject(string key)
        {
            return _storage.GetReadOnlyJsonObject(key);
        }

        public ReadOnlyJsonArray GetReadOnlyJsonArray(string key)
        {
            return _storage.GetReadOnlyJsonArray(key);
        }


        public bool IsNull(string key)
        {
            return _storage.IsNull(key);
        }


        public object Opt(string key)
        {
            return _storage.Opt(key);
        }

        public TType Opt<TType>(string key, TType fallback, out bool isFallbackReturned)
        {
            return _storage.Opt(key, fallback, out isFallbackReturned);
        }

        public TType Opt<TType>(string key, TType fallback)
        {
            return _storage.Opt(key, fallback);
        }

        public TType Opt<TType>(string key, out bool isFallbackReturned)
        {
            return _storage.Opt<TType>(key, out isFallbackReturned);
        }

        public TType Opt<TType>(string key)
        {
            return _storage.Opt<TType>(key);
        }


        public bool OptBoolean(string key, bool fallback, out bool isFallbackReturned)
        {
            return _storage.OptBoolean(key, fallback, out isFallbackReturned);
        }

        public bool OptBoolean(string key, bool fallback)
        {
            return _storage.OptBoolean(key, fallback);
        }

        public bool OptBoolean(string key, out bool isFallbackReturned)
        {
            return _storage.OptBoolean(key,  out isFallbackReturned);
        }

        public bool OptBoolean(string key)
        {
            return _storage.OptBoolean(key);
        }


        public byte OptInt16(string key, byte fallback, out bool isFallbackReturned)
        {
            return _storage.OptInt16(key, fallback, out isFallbackReturned);
        }

        public byte OptInt16(string key, byte fallback)
        {
            return _storage.OptInt16(key, fallback);
        }

        public byte OptInt16(string key, out bool isFallbackReturned)
        {
            return _storage.OptInt16(key, out isFallbackReturned);
        }

        public byte OptInt16(string key)
        {
            return _storage.OptInt16(key);
        }


        public int OptInt32(string key, int fallback, out bool isFallbackReturned)
        {
            return _storage.OptInt32(key, fallback, out isFallbackReturned);
        }

        public int OptInt32(string key, int fallback)
        {
            return _storage.OptInt32(key, fallback);
        }

        public int OptInt32(string key, out bool isFallbackReturned)
        {
            return _storage.OptInt32(key, out isFallbackReturned);
        }

        public int OptInt32(string key)
        {
            return _storage.OptInt32(key);
        }


        public long OptInt64(string key, long fallback, out bool isFallbackReturned)
        {
            return _storage.OptInt64(key, fallback, out isFallbackReturned);
        }

        public long OptInt64(string key, long fallback)
        {
            return _storage.OptInt64(key, fallback);
        }

        public long OptInt64(string key, out bool isFallbackReturned)
        {
            return _storage.OptInt64(key, out isFallbackReturned);
        }

        public long OptInt64(string key)
        {
            return _storage.OptInt64(key);
        }


        public decimal OptDecimal(string key, decimal fallback, out bool isFallbackReturned)
        {
            return _storage.OptDecimal(key, fallback, out isFallbackReturned);
        }

        public decimal OptDecimal(string key, decimal fallback)
        {
            return _storage.OptDecimal(key, fallback);
        }

        public decimal OptDecimal(string key, out bool isFallbackReturned)
        {
            return _storage.OptDecimal(key, out isFallbackReturned);
        }

        public decimal OptDecimal(string key)
        {
            return _storage.OptDecimal(key);
        }


        public float OptSingle(string key, float fallback, out bool isFallbackReturned)
        {
            return _storage.OptSingle(key, fallback, out isFallbackReturned);
        }

        public float OptSingle(string key, float fallback)
        {
            return _storage.OptSingle(key, fallback);
        }

        public float OptSingle(string key, out bool isFallbackReturned)
        {
            return _storage.OptSingle(key, out isFallbackReturned);
        }

        public float OptSingle(string key)
        {
            return _storage.OptSingle(key);
        }


        public double OptDouble(string key, double fallback, out bool isFallbackReturned)
        {
            return _storage.OptDouble(key, fallback, out isFallbackReturned);
        }

        public double OptDouble(string key, double fallback)
        {
            return _storage.OptDouble(key, fallback);
        }

        public double OptDouble(string key, out bool isFallbackReturned)
        {
            return _storage.OptDouble(key, out isFallbackReturned);
        }

        public double OptDouble(string key)
        {
            return _storage.OptDouble(key);
        }


        public string OptString(string key, string fallback, out bool isFallbackReturned)
        {
            return _storage.OptString(key, fallback, out isFallbackReturned);
        }

        public string OptString(string key, string fallback)
        {
            return _storage.OptString(key, fallback);
        }

        public string OptString(string key, out bool isFallbackReturned)
        {
            return _storage.OptString(key, out isFallbackReturned);
        }

        public string OptString(string key)
        {
            return _storage.OptString(key);
        }


        public DateTime OptDateTime(string key, DateTime fallback, out bool isFallbackReturned)
        {
            return _storage.OptDateTime(key, fallback, out isFallbackReturned);
        }

        public DateTime OptDateTime(string key, DateTime fallback)
        {
            return _storage.OptDateTime(key, fallback);
        }

        public DateTime OptDateTime(string key, out bool isFallbackReturned)
        {
            return _storage.OptDateTime(key, out isFallbackReturned);
        }

        public DateTime OptDateTime(string key)
        {
            return _storage.OptDateTime(key);
        }


        public JsonObject OptJsonObject(string key, JsonObject fallback, out bool isFallbackReturned)
        {
            return _storage.OptJsonObject(key, fallback, out isFallbackReturned);
        }

        public JsonObject OptJsonObject(string key, JsonObject fallback)
        {
            return _storage.OptJsonObject(key, fallback);
        }

        public JsonObject OptJsonObject(string key, out bool isFallbackReturned)
        {
            return _storage.OptJsonObject(key, out isFallbackReturned);
        }

        public JsonObject OptJsonObject(string key)
        {
            return _storage.OptJsonObject(key);
        }


        public JsonArray OptJsonArray(string key, JsonArray fallback, out bool isFallbackReturned)
        {
            return _storage.OptJsonArray(key, fallback, out isFallbackReturned);
        }

        public JsonArray OptJsonArray(string key, JsonArray fallback)
        {
            return _storage.OptJsonArray(key, fallback);
        }

        public JsonArray OptJsonArray(string key, out bool isFallbackReturned)
        {
            return _storage.OptJsonArray(key, out isFallbackReturned);
        }

        public JsonArray OptJsonArray(string key)
        {
            return _storage.OptJsonArray(key);
        }



        public ExpandoObject ToExpandoObject()
        {
            return _storage.ToExpandoObject();
        }

        #endregion
    }
}
