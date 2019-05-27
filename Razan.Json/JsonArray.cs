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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Razan.Json
{
    public class JsonArray : IJsonArray<object>
    {
        #region constructors

        public JsonArray()
        {
            _storage = new List<object>();

            AsReadOnly = new ReadOnlyJsonArray(this);
        }
        public JsonArray(bool disableJsonDataConvertion) : this()
        {
            _disableJsonDataConvertion = disableJsonDataConvertion;
        }

        public JsonArray(IEnumerable<object> collection)
        {
            _storage = new List<object>(collection);

            AsReadOnly = new ReadOnlyJsonArray(this);
        }
        public JsonArray(IEnumerable<object> collection, bool disableJsonDataConvertion)
            : this(collection)
        {
            _disableJsonDataConvertion = disableJsonDataConvertion;
        }
        public JsonArray(string jsonArrayString)
        {
            _stringified = jsonArrayString;

            AsReadOnly = new ReadOnlyJsonArray(this);
        }
        public JsonArray(string jsonArrayString, bool disableJsonDataConvertion)
            : this(jsonArrayString)
        {
            _disableJsonDataConvertion = disableJsonDataConvertion;
        }

        #endregion

        #region private fields

        private List<object> _storage;

        private string _stringified;
        private EncodingBases _stringifiedEncodingModes;
        private bool _stringifiedSafeTypeReversible;

        private readonly ConcurrentDictionary<Type, WeakReference> _jsonArraysCache = new ConcurrentDictionary<Type, WeakReference>();

        private object _arrayCache;

        private bool _disableJsonDataConvertion;
        private Type _underlyingArrayType;

        #endregion

        #region properties

        public bool IsStringified => _stringified != null;

        public bool DisableJsonDataConvertion
        {
            get => _disableJsonDataConvertion;
            set
            {
                if (value && !_disableJsonDataConvertion)
                {
                    _disableJsonDataConvertion = true;

                    for (var i = 0; i < Count; i++)
                    {
                        var value2 = this[i];
                        var value3 = JsonObject.GetJsonEquivalent(true);

                        if (value2 != value3)
                        {
                            GetStorage().RemoveAt(i);

                            GetStorage().Insert(i, value3);
                        }
                    }
                }
                else
                {
                    _disableJsonDataConvertion = value;
                }
            }
        }

        public ReadOnlyJsonArray AsReadOnly { get; }

        public Type UnderlyingArrayType
        {
            get { GetStorage(); return _underlyingArrayType; }
            private set => _underlyingArrayType = value;
        }

        public string FileName { get; set; }

        #endregion

        #region IList<object> implementation

        public int IndexOf(object item)
        {
            return GetStorage().IndexOf(item);
        }

        public void Insert(int index, object item)
        {
            GetStorage().Insert(index, ItemPushed(_disableJsonDataConvertion ? item : JsonObject.GetJsonEquivalent(item), true));
        }

        public void RemoveAt(int index)
        {
            var item = this[index];

            GetStorage().RemoveAt(index);

            ItemPulled(item);
        }

        public void Add(object item)
        {
            GetStorage().Add(ItemPushed(_disableJsonDataConvertion ? item : JsonObject.GetJsonEquivalent(item), true));
        }

        public void Clear()
        {
            GetStorage().Clear();

            _arrayCache = null;

            UnderlyingArrayType = null;
        }

        public bool Contains(object item)
        {
            return GetStorage().Contains(item);
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            GetStorage().CopyTo(array, arrayIndex);
        }

        public bool Remove(object item)
        {
            if (GetStorage().Remove(item))
            {
                ItemPulled(item);

                return true;
            }

            return false;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return GetStorage().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)GetStorage()).GetEnumerator();
        }

        public int Count => GetStorage().Count;

        public bool IsReadOnly => false;

        public object this[int index]
        {
            get => GetStorage()[index];
            set
            {
                var item = GetStorage()[index];

                GetStorage()[index] = value;

                if ((item == null ^ value == null) || (item != null && item.GetType() != value.GetType()))
                {
                    ItemPulled(item);
                }
            }
        }

        #endregion

        #region private methods

        private List<object> GetStorage()
        {
            if (_storage != null) return _storage;

            var jsonArray = Decode(_stringified);

            var result = new List<object>(jsonArray);

            UnderlyingArrayType = jsonArray.UnderlyingArrayType;

            _stringified = null;

            return _storage = result;
        }

        private object ItemPushed(object item, bool clearArrayCache)
        {
            if (clearArrayCache) { _arrayCache = null; }

            var type = item?.GetType();

            if (type != null)
            {
                if (UnderlyingArrayType == null)
                {
                    UnderlyingArrayType = type;
                }
                else if (UnderlyingArrayType != type)
                {
                    UnderlyingArrayType = typeof(object);
                }
            }
            else
            {
                UnderlyingArrayType = typeof(object);
            }

            return item;
        }

        private void ItemPulled(object item)
        {
            _arrayCache = null;

            if (Count == 0)
            {
                UnderlyingArrayType = null;
            }
            else
            {
                foreach (var item2 in this)
                {
                    ItemPushed(item2, false);

                    if (UnderlyingArrayType == typeof(object)) { break; }
                }
            }
        }

        #endregion

        #region override methods

        public override string ToString()
        {
            return ToString(EncodingBases.Default);
        }

        #endregion

        #region methods

        public string ToString(EncodingBases encodingModes, bool safeTypeReversible)
        {
            if (IsStringified)
            {
                if (IsStringified && _stringifiedEncodingModes == encodingModes && _stringifiedSafeTypeReversible == safeTypeReversible)
                {
                    return _stringified;
                }
            }

            return JsonObject.EncodeEnumerable(this, encodingModes, safeTypeReversible);
        }

        public string ToString(EncodingBases encodingModes)
        {
            return ToString(encodingModes, false);
        }

        public void SetContent(IEnumerable<object> collection)
        {
            Clear();

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        public JsonArray<T> AsJsonArray<T>()
        {
            if (_jsonArraysCache.TryGetValue(typeof(T), out var weakReference))
            {
                if (weakReference.Target != null)
                {
                    return (JsonArray<T>)weakReference.Target;
                }
            }

            JsonArray<T> result = null;

            _jsonArraysCache.AddOrUpdate(
                typeof(T),
                type => new WeakReference(result = new JsonArray<T>(this)),
                (type, weakReference2) =>
                {
                    if (weakReference2.Target == null)
                        return new WeakReference(result = new JsonArray<T>(this));

                    result = (JsonArray<T>)weakReference2.Target;

                    return weakReference2;
                });

            return result;
        }

        public JsonArray<T> ToJsonArray<T>(Func<object, T> castFunc)
        {
            if (UnderlyingArrayType == typeof(T)) return AsJsonArray<T>();

            for (var i = 0; i < Count; i++)
            {
                if (this[i] is T) continue;

                this[i] = castFunc(this[i]);
            }

            return AsJsonArray<T>();
        }

        public T[] AsArray<T>()
        {
            if (_arrayCache == null || !(_arrayCache is T[]))
            {
                if (Count == 0)
                {
                    _arrayCache = new T[] { };
                }
                else if (UnderlyingArrayType == typeof(T))
                {
                    _arrayCache = this.Select(item => (T)item).ToArray();
                }
                else
                {
                    //throw new InvalidCastException($"Underlying array type does not match to '{typeof(T).FullName}'");

                    return null;
                }
            }

            return (T[])_arrayCache;
        }

        public void Stringify(EncodingBases encodingModes, bool safeTypeReversible)
        {
            _stringifiedEncodingModes = encodingModes;
            _stringifiedSafeTypeReversible = safeTypeReversible;

            _stringified = ToString(_stringifiedEncodingModes, _stringifiedSafeTypeReversible);

            _storage = null;
        }
        public void Stringify(EncodingBases encodingModes)
        {
            Stringify(encodingModes, false);
        }
        public void Stringify(bool safeTypeReversible)
        {
            Stringify(EncodingBases.Default, safeTypeReversible);
        }
        public void Stringify()
        {
            Stringify(EncodingBases.Default, false);
        }

        public static JsonArray Decode(string jsonArrayString)
        {
            if (TryDecode(jsonArrayString, out var result))
            {
                return result;
            }

            throw new InvalidCastException("Json array string is not well structured!");
        }

        public static bool TryDecode(string jsonArrayString, out JsonArray result)
        {
            var lastIndex = -1;
            if (JsonObject.TryGetNextCharacter(jsonArrayString, ref lastIndex, out var character, true) && character == '[')
            {
                result = DecodeNextJsonArray(jsonArrayString, ref lastIndex);

                return true;
            }

            result = null;

            return false;
        }

        public static bool IsValid(string jsonArrayString)
        {
            if (TryDecode(jsonArrayString, out _))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region internal methods

        internal static JsonArray DecodeNextJsonArray(string jsonString, ref int lastIndex)
        {
            var jsonArray = new JsonArray();

            while (JsonObject.TryGetNextCharacter(jsonString, ref lastIndex, out var character))
            {
                if (character == ']') { break; }

                if (character == ',') { continue; }

                --lastIndex;

                jsonArray.Add(JsonObject.DecodeNextValue(jsonString, ref lastIndex));
            }

            return jsonArray;
        }

        #endregion

        public static JsonArray ReadFrom(string fileName)
        {
            var result = (JsonArray)JsonObject.DecodeObject(File.ReadAllText(fileName));
            result.FileName = fileName;

            return result;
        }
        public static JsonArray TryReadFrom(string fileName, JsonArray @default)
        {
            try
            {
                return ReadFrom(fileName);
            }
            catch (Exception)
            {
                if (@default != null)
                {
                    @default.FileName = fileName;
                }

                return @default;
            }
        }

        public void WriteTo(string fileName, EncodingBases encodingModes, bool safeTypeReversible)
        {
            var fileInfo = new FileInfo(fileName);

            Debug.Assert(fileInfo.Directory != null, "fileInfo.Directory != null");

            if (!fileInfo.Directory.Exists) { fileInfo.Directory.Create(); }

            File.WriteAllText(fileName, ToString(encodingModes, safeTypeReversible));
        }
        public void WriteTo(string fileName, EncodingBases encodingModes)
        {
            WriteTo(fileName, encodingModes, false);
        }
        public void WriteTo(string fileName, bool safeTypeReversible)
        {
            WriteTo(fileName, EncodingBases.Default, safeTypeReversible);
        }
        public void WriteTo(string fileName)
        {
            WriteTo(fileName, false);
        }

        public void SaveToFileName(EncodingBases encodingModes, bool safeTypeReversible)
        {
            WriteTo(FileName, encodingModes, safeTypeReversible);
        }
        public void SaveToFileName(EncodingBases encodingModes)
        {
            WriteTo(FileName, encodingModes);
        }
        public void SaveToFileName(bool safeTypeReversible)
        {
            WriteTo(FileName, safeTypeReversible);
        }
        public void SaveToFileName()
        {
            WriteTo(FileName);
        }
    }
}
