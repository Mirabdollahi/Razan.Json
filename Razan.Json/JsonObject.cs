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
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Razan.Json
{
    public class JsonObject : DynamicObject, IDictionary<string, object>
    {
        #region constructors

        public JsonObject(EncodingBases encoding)
        {
            Encoding = encoding;
            _storage = new ConcurrentDictionary<string, object>();
            _disableJsonDataConvertion = false;
            AsReadOnly = new ReadOnlyJsonObject(this);
        }

        public JsonObject() : this(EncodingBases.Default) { }

        public JsonObject(IEnumerable<KeyValuePair<string, object>> dictionary, EncodingBases encoding) : this(encoding)
        {
            foreach (var item in dictionary)
            {
                Add(item);
            }
        }

        public JsonObject(IEnumerable<KeyValuePair<string, object>> dictionary) : this(dictionary, EncodingBases.Default) { }

        public JsonObject(string jsonObjectString, EncodingBases encoding)
        {
            Encoding = encoding;
            _stringified = jsonObjectString;
            _disableJsonDataConvertion = false;
            AsReadOnly = new ReadOnlyJsonObject(this);
        }

        public JsonObject(string jsonObjectString) : this(jsonObjectString, EncodingBases.Default) { }

        public JsonObject(bool disableJsonDataConvertion, EncodingBases encoding) : this(encoding)
        {
            _disableJsonDataConvertion = disableJsonDataConvertion;
        }

        public JsonObject(bool disableJsonDataConvertion) : this(disableJsonDataConvertion, EncodingBases.Default) { }

        #endregion

        #region private static fields

        private static readonly char[] WhiteSpaceCharacters = { ' ', '\r', '\n', '\t' };

        private static readonly CultureInfo EnglishUsCultureInfo = CultureInfo.CreateSpecificCulture("en-US");

        #endregion

        #region private fields


        private IDictionary<string, object> _storage;

        private string _stringified;
        private EncodingBases _stringifiedEncodingModes;
        private bool _stringifiedSafeTypeReversible;

        #endregion

        #region public fields

        public readonly object SyncRoot = new object();

        #endregion

        #region DynamicObject overrides


        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return GetStorage().Keys;
        }


        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return GetStorage().TryGetValue(binder.Name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            GetStorage()[binder.Name] = DisableJsonDataConvertion ? value : GetJsonEquivalent(value);

            return true;
        }

        #endregion

        #region properties


        private bool _disableJsonDataConvertion;

        public bool DisableJsonDataConvertion
        {
            get
            {
                return _disableJsonDataConvertion;
            }
            set
            {
                if (value && !_disableJsonDataConvertion)
                {
                    _disableJsonDataConvertion = true;

                    foreach (var item in this)
                    {
                        var value2 = GetJsonEquivalent(true);
                        this[item.Key] = value2;
                    }
                }
                else
                {
                    _disableJsonDataConvertion = value;
                }
            }
        }


        public dynamic AsDynamic => this;

        public ReadOnlyJsonObject AsReadOnly { get; }
        public string FileName { get; set; }

        public EncodingBases Encoding { get; set; }


        public bool IsStringified => _stringified != null;

        #endregion

        #region private properties


        private IDictionary<string, object> GetStorage()
        {
            if (_storage != null) return _storage;

            var result = new ConcurrentDictionary<string, object>(Decode(_stringified));

            _stringified = null;

            return _storage = result;
        }

        #endregion

        #region private static methods


        private static bool IsAnonymous(Type type)
        {
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            if (Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) &&
                (type.IsGenericType || !type.GetProperties().Any()))
            {
                if (type.Name.Contains("AnonymousType") &&
                    (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) &&
                    (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic)
                {
                    return true;
                }
            }

            return false;
        }


        private static bool TryGetKeyValues(object value, Type type, out IEnumerable<KeyValuePair<string, object>> keyValues)
        {
            if (value == null)
            {
                keyValues = null;

                return false;
            }

            if (value is IDictionary<string, object> values)
            {
                keyValues = values;

                return true;
            }

            if (type == null)
            {
                type = value.GetType();
            }

            if (value is IDictionary valueDictionary && type.GetGenericArguments()[0] == typeof(string))
            {
                var dictionary = new Dictionary<string, object>();

                var a = valueDictionary.GetEnumerator();

                while (a.MoveNext())
                {
                    dictionary.Add((string)a.Key, a.Value);
                }

                keyValues = dictionary;

                return true;
            }

            if (IsAnonymous(type))
            {
                keyValues = type
                    .GetProperties()
                    .Select(property => new KeyValuePair<string, object>(property.Name, property.GetValue(value, null)));

                return true;
            }

            keyValues = null;

            return false;
        }

        private static bool TryGetKeyValues(object value, out IEnumerable<KeyValuePair<string, object>> keyValues)
        {
            return TryGetKeyValues(value, null, out keyValues);
        }


        private static bool TryGetEnumerable(object value, out IEnumerable enumerable)
        {
            if (value == null || value is string)
            {
                enumerable = null;

                return false;
            }

            if (value is IEnumerable)
            {
                enumerable = (IEnumerable)value;

                return true;
            }

            enumerable = null;

            return false;
        }


        private static string EncodeKeyValues(IEnumerable<KeyValuePair<string, object>> keyValues, EncodingBases encodingModes, bool safeTypeReversible)
        {
            var sb = new StringBuilder();
            var useSeparator = false;

            foreach (var item in keyValues)
            {
                if (useSeparator) { sb.Append(","); }

                sb.Append(
                    $"{EncodeString(item.Key, encodingModes)}:{Encode(item.Value, encodingModes, safeTypeReversible)}");

                if (!useSeparator) { useSeparator = true; }
            }

            return "{" + sb + "}";
        }


        internal static string EncodeEnumerable(IEnumerable enumerable, EncodingBases encodingModes, bool safeTypeReversible)
        {
            var sb = new StringBuilder();
            var useSeparator = false;

            var enumerator = enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (useSeparator) { sb.Append(","); }

                sb.Append(Encode(enumerator.Current, encodingModes, safeTypeReversible));

                useSeparator = true;
            }

            return "[" + sb + "]";
        }


        private static string EncodeJsonObject(JsonObject jsonObject, EncodingBases encodingModes, bool safeTypeReversible = false)
        {
            return EncodeKeyValues(jsonObject, encodingModes, safeTypeReversible);
        }


        private static string EncodeString(string value, EncodingBases encodingModes)
        {
            if (encodingModes == EncodingBases.Default) { encodingModes = EncodingBases.Utf16; }

            var encodedString = new StringBuilder();

            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        encodedString.Append("\\" + '\\'); break;
                    case '"':
                        encodedString.Append("\\" + '"'); break;
                    case '/':
                        encodedString.Append("\\" + '/'); break;
                    case '\b':
                        encodedString.Append("\\" + 'b'); break;
                    case '\f':
                        encodedString.Append("\\" + 'f'); break;
                    case '\t':
                        encodedString.Append("\\" + 't'); break;
                    case '\n':
                        encodedString.Append("\\" + 'n'); break;
                    case '\r':
                        encodedString.Append("\\" + 'r'); break;
                    default:
                        var codepoint = Convert.ToInt32(character);

                        if (encodingModes == EncodingBases.Utf32 || codepoint >= 32 && codepoint <= 126)
                        {
                            encodedString.Append(character);
                        }
                        else if (encodingModes == EncodingBases.Utf16)
                        {
                            encodedString.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                        }

                        break;
                }
            }

            return $"\"{encodedString}\"";
        }



        private static string DecodeString(string stringValue)
        {
            var decodedString = string.Empty;
            for (var i = 0; i < stringValue.Length; i++)
            {
                var character = stringValue[i];

                if (character == '\\' && i < stringValue.Length - 1)
                {
                    var nextCharacter = stringValue[i + 1];

                    switch (nextCharacter)
                    {
                        case '\\':
                            decodedString += '\\'; i++; break;
                        case '"':
                            decodedString += '"'; i++; break;
                        case '/':
                            decodedString += '/'; i++; break;
                        case 'b':
                            decodedString += '\b'; i++; break;
                        case 'f':
                            decodedString += '\f'; i++; break;
                        case 't':
                            decodedString += '\t'; i++; break;
                        case 'n':
                            decodedString += '\n'; i++; break;
                        case 'r':
                            decodedString += '\r'; i++; break;
                        default:
                            if (nextCharacter == 'u' && i < stringValue.Length - 5)
                            {
                                if (!uint.TryParse(new string(stringValue.ToCharArray(), i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint codePoint))
                                {
                                    decodedString += character;
                                }
                                else
                                {
                                    decodedString += char.ConvertFromUtf32((int)codePoint);

                                    i += 5;
                                }
                            }
                            else
                            {
                                decodedString += character;
                            }

                            break;
                    }
                }
                else
                {
                    decodedString += character;
                }
            }

            return decodedString;
        }

        internal static bool TryGetNextCharacter(string jsonString, ref int lastIndex, out char character, bool ignoreComments)
        {
            character = default(char);

            if (string.IsNullOrEmpty(jsonString)) { return false; }

            if (ignoreComments)
            {
                while (++lastIndex < jsonString.Length)
                {
                    var c = jsonString[lastIndex];

                    if (WhiteSpaceCharacters.Contains(c)) { continue; }

                    if (c == '/')
                    {
                        GetNextComment(jsonString, ref lastIndex);

                        return TryGetNextCharacter(jsonString, ref lastIndex, out character, true);
                    }

                    character = c;

                    return true;
                }
            }
            else
            {
                while (++lastIndex < jsonString.Length)
                {
                    var c = jsonString[lastIndex];

                    if (WhiteSpaceCharacters.Contains(c)) { continue; }

                    character = c;

                    return true;
                }
            }

            return false;
        }

        internal static bool TryGetNextCharacter(string jsonString, ref int lastIndex, out char character)
        {
            return TryGetNextCharacter(jsonString, ref lastIndex, out character, false);
        }

        private static string GetNextComment(string jsonString, ref int lastIndex)
        {
            if (++lastIndex < jsonString.Length)
            {
                string comment;
                var nextChar = jsonString[lastIndex];

                if (nextChar == '*')
                {
                    var endingPairIndex = jsonString.IndexOf("*/", lastIndex, StringComparison.Ordinal);

                    if (endingPairIndex != -1)
                    {
                        comment = jsonString.Substring(lastIndex + 1, endingPairIndex - lastIndex - 1);

                        lastIndex = endingPairIndex + 1;

                        return comment;
                    }

                    throw new InvalidCastException("Json string is not well structured!");
                }

                if (nextChar == '/')
                {
                    var nextCharacterAfterCommentIndex = jsonString.IndexOfAny(new[] { '\r', '\n' }, lastIndex);

                    if (nextCharacterAfterCommentIndex == -1)
                    {
                        lastIndex = jsonString.Length - 1;

                        return string.Empty;
                    }

                    comment = jsonString.Substring(lastIndex + 1, nextCharacterAfterCommentIndex - lastIndex - 1);

                    lastIndex = nextCharacterAfterCommentIndex;

                    if (jsonString.Length - 1 > nextCharacterAfterCommentIndex + 1 &&
                        jsonString[nextCharacterAfterCommentIndex + 1] == '\n' &&
                        jsonString[nextCharacterAfterCommentIndex] == '\r')
                    {
                        lastIndex = +1;
                    }

                    return comment;
                }

                throw new InvalidCastException("Json string is not well structured!");
            }

            throw new InvalidCastException("Json string is not well structured!");
        }


        private static string GetNextString(string jsonString, ref int lastIndex)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new InvalidCastException("Json string is not well structured!");
            }

            var startIndex = lastIndex + 1;
            var isEscapeCharacter = false;
            while (++lastIndex < jsonString.Length)
            {
                var c = jsonString[lastIndex];

                switch (c)
                {
                    case '\"':
                        if (!isEscapeCharacter)
                        {
                            return jsonString.Substring(startIndex, lastIndex - startIndex);
                        }
                        else
                        {
                            isEscapeCharacter = false;
                        }

                        break;
                    case '\\':
                        isEscapeCharacter = true;

                        break;
                    default:
                        isEscapeCharacter = false;

                        break;
                }
            }

            throw new InvalidCastException("Json string is not well structured!");
        }

        private static bool TryDecodeNextKeyValue(string jsonString, ref int lastIndex, out KeyValuePair<string, object> keyValue)
        {
            char character;
            string key = null;
            var isColonPassed = false;

            while (key == null && TryGetNextCharacter(jsonString, ref lastIndex, out character))
            {
                switch (character)
                {
                    case '"':
                        key = DecodeString(GetNextString(jsonString, ref lastIndex));

                        break;
                    case '/': // Ignoring comment
                        GetNextComment(jsonString, ref lastIndex);

                        //throw new NotImplementedException();
                        break;
                    case ',':
                        continue;
                    case '}':
                        keyValue = default(KeyValuePair<string, object>);

                        return false;
                    default:
                        throw new InvalidCastException("Json string is not well structured!");
                }
            }

            if (key == null)
            {
                if (lastIndex == jsonString.Length)
                {
                    keyValue = default(KeyValuePair<string, object>);

                    return false;
                }

                throw new InvalidCastException("Json string is not well structured!");
            }

            while (!isColonPassed && TryGetNextCharacter(jsonString, ref lastIndex, out character))
            {
                switch (character)
                {
                    case ':':
                        isColonPassed = true;

                        break;
                    case '/': // Ignoring comment
                        GetNextComment(jsonString, ref lastIndex);

                        //throw new NotImplementedException();
                        break;
                    default:
                        throw new InvalidCastException("Json string is not well structured!");
                }
            }

            if (!isColonPassed) { throw new InvalidCastException("Json string is not well structured!"); }

            var value = DecodeNextValue(jsonString, ref lastIndex);

            keyValue = new KeyValuePair<string, object>(key, value);

            return true;
        }


        private static void DecodeNextJsonObject(string jsonString, ref int lastIndex, ref JsonObject jsonObject)
        {
            if (jsonObject == null) { jsonObject = new JsonObject(); }

            while (TryDecodeNextKeyValue(jsonString, ref lastIndex, out KeyValuePair<string, object> keyValue))
            {
                jsonObject.Add(keyValue);
            }
        }

        private static JsonObject DecodeNextJsonObject(string jsonString, ref int lastIndex)
        {
            JsonObject jsonObject = null;
            DecodeNextJsonObject(jsonString, ref lastIndex, ref jsonObject);

            return jsonObject;
        }


        private static string GetNextValue(string jsonString, ref int lastIndex)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                throw new InvalidCastException("Json string is not well structured!");
            }

            var aa = new[] { ',', '}', ']', '/' };

            var startIndex = lastIndex + 1;
            while (++lastIndex < jsonString.Length)
            {
                var c = jsonString[lastIndex];

                if (WhiteSpaceCharacters.Contains(c) || aa.Contains(c))
                {
                    --lastIndex;

                    return jsonString.Substring(startIndex, lastIndex + 1 - startIndex);
                }
            }

            throw new InvalidCastException("Json string is not well structured!");
        }


        internal static object DecodeNextValue(string jsonString, ref int lastIndex)
        {
            if (TryGetNextCharacter(jsonString, ref lastIndex, out char character))
            {
                if (character == '[')
                {
                    var index = lastIndex;

                    var innerPairs = 0;
                    while (TryGetNextCharacter(jsonString, ref lastIndex, out char character2))
                    {
                        if (character2 == '[')
                        {
                            innerPairs++;
                            continue;
                        }

                        if (character2 != ']') continue;

                        if (innerPairs == 0)
                            return new JsonArray(jsonString.Substring(index, lastIndex - index + 1));

                        innerPairs--;
                    }

                    throw new InvalidCastException("Json array string is not well structured!");
                }

                switch (character)
                {
                    case '"':
                        var a = DecodeString(GetNextString(jsonString, ref lastIndex));

                        if (a.Length == 19 && a[4] == '-' && a[7] == '-' && a[10] == 'T' && a[13] == ':' && a[16] == ':')
                        {
                            if (DateTime.TryParse(a, EnglishUsCultureInfo, DateTimeStyles.None, out var dateTime)) { return dateTime; }
                        }

                        return a;
                    case '{':
                        return DecodeNextJsonObject(jsonString, ref lastIndex);
                    case '/': // Ignoring comment
                        GetNextComment(jsonString, ref lastIndex);

                        //throw new NotImplementedException();
                        //break;

                        return DecodeNextValue(jsonString, ref lastIndex);
                    default:
                        --lastIndex;

                        var strValue = GetNextValue(jsonString, ref lastIndex);

                        var strType = string.Empty;
                        if (TryGetNextCharacter(jsonString, ref lastIndex, out character))
                        {
                            if (character == '/')
                            {
                                strType = GetNextComment(jsonString, ref lastIndex);
                            }
                            else
                            {
                                --lastIndex;
                            }
                        }

                        switch (strType)
                        {
                            case "sbyte": return sbyte.Parse(strValue);
                            case "byte": return byte.Parse(strValue);
                            case "char": return char.Parse(strValue);
                            case "short": return short.Parse(strValue);
                            case "ushort": return ushort.Parse(strValue);
                            case "int": return int.Parse(strValue);
                            case "uint": return uint.Parse(strValue);
                            case "long": return long.Parse(strValue);
                            case "ulong": return ulong.Parse(strValue);
                            case "decimal": return decimal.Parse(strValue.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                            case "double": return double.Parse(strValue.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                            case "float": return float.Parse(strValue.Replace(".", Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                        }

                        if (strValue.Equals("true")) { return true; }
                        else if (strValue.Equals("false")) { return false; }
                        else if (strValue.Equals("null")) { return null; }
                        else if (strValue.All(v => "1234567890.".Contains(v)) && !strValue.StartsWith(".") && !strValue.EndsWith(".") && strValue.Count(v => v == '.') <= 1)
                        {
                            if (strValue.Contains('.'))
                            {
                                if (decimal.TryParse(strValue, out decimal decimalValue)) { return decimalValue; }

                                if (float.TryParse(strValue, out float floatValue)) { return floatValue; }

                                if (double.TryParse(strValue, out double doubleValue)) { return doubleValue; }

                                throw new InvalidCastException(
                                    $"Value '{strValue}' cannot be parsed to a number with floating point.");
                            }

                            if (sbyte.TryParse(strValue, out sbyte sbyteValue)) { return sbyteValue; }

                            if (byte.TryParse(strValue, out byte byteValue)) { return byteValue; }

                            if (short.TryParse(strValue, out short shortValue)) { return shortValue; }

                            if (ushort.TryParse(strValue, out ushort ushortValue)) { return ushortValue; }

                            if (int.TryParse(strValue, out int intValue)) { return intValue; }

                            if (uint.TryParse(strValue, out uint uintValue)) { return uintValue; }

                            if (long.TryParse(strValue, out long longValue)) { return longValue; }

                            if (ulong.TryParse(strValue, out ulong ulongValue)) { return ulongValue; }

                            throw new InvalidCastException(
                                $"Value '{strValue}' cannot be parsed to an integral number.");
                        }

                        throw new InvalidCastException($"Value '{strValue}' cannot be parsed to any data type.");
                }
            }

            throw new InvalidCastException("Json string is not well structured!");
        }


        private static object ToArray(JsonArray jsonArray)
        {
            var array = new List<object>();

            foreach (var item in jsonArray)
            {
                if (item is JsonObject jsonObject)
                {
                    array.Add(jsonObject.ToExpandoObject());
                }
                else
                {
                    if (item is JsonArray jsonArray2) array.Add(ToArray(jsonArray2));
                }
            }

            return array.ToArray();
        }

        #endregion

        #region public static methods


        public static string Encode(object value, EncodingBases encodingModes, bool safeTypeReversible = false)
        {
            var jsonObject =
                value as JsonObject ??
                (value as IConvertibleToJsonObject)?.ToJsonObject();
            if (jsonObject != null) { return jsonObject.ToString(encodingModes, safeTypeReversible); }

            var jsonArray =
                value as IJsonArray ??
                (value as IConvertibleToJsonArray)?.ToJsonArray();
            if (jsonArray != null) { return jsonArray.ToString(encodingModes, safeTypeReversible); }

            if (value is IConvertibleToJsonString convertibleToJsonString)
                return convertibleToJsonString.ToJsonString(encodingModes, safeTypeReversible);

            if (value is string str) return EncodeString(str, encodingModes);

            if (value is DateTime) return string.Format(EnglishUsCultureInfo, "\"{0:s}\"", value);

            if (value is bool b) { return b ? "true" : "false"; }

            if (safeTypeReversible)
            {
                if (value is sbyte) { return value + "/*sbyte*/"; }
                if (value is byte) { return value + "/*byte*/"; }
                if (value is char) { return value + "/*char*/"; }
                if (value is short) { return value + "/*short*/"; }
                if (value is ushort) { return value + "/*ushort*/"; }
                if (value is int) { return value + "/*int*/"; }
                if (value is uint) { return value + "/*uint*/"; }
                if (value is long) { return value + "/*long*/"; }
                if (value is ulong) { return value + "/*ulong*/"; }

                if (value is decimal) { return value.ToString().Replace(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".") + "/*decimal*/"; }
                if (value is double) { return value.ToString().Replace(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".") + "/*double*/"; }
                if (value is float) { return value.ToString().Replace(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".") + "/*float*/"; }
            }

            if (value is sbyte ||
                value is byte ||
                value is char ||
                value is short ||
                value is ushort ||
                value is int ||
                value is uint ||
                value is long ||
                value is ulong) { return value.ToString(); }

            if (value is decimal ||
                value is double ||
                value is float) { return value.ToString().Replace(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."); }

            if (TryGetKeyValues(value, out var keyValues)) { return EncodeKeyValues(keyValues, encodingModes, safeTypeReversible); }

            if (value == null) { return "null"; }

            var valueType = value.GetType();
            if (valueType.IsEnum) { return Convert.ChangeType(value, Enum.GetUnderlyingType(valueType)).ToString(); }

            var jsonEquivalentValue = GetJsonEquivalent(value);
            if (value != jsonEquivalentValue) { return Encode(jsonEquivalentValue, encodingModes, safeTypeReversible); }

            throw new InvalidCastException(
                $"Error: Type {value.GetType().FullName} cannot be converted to json string.");
        }

        public static string Encode(object value, bool safeTypeReversible = false)
        {
            return Encode(value, EncodingBases.Default, safeTypeReversible);
        }


        public static object DecodeObject(string jsonString)
        {
            var lastIndex = -1;
            return DecodeNextValue(jsonString, ref lastIndex);
        }


        public static JsonObject Decode(string jsonObjectString)
        {
            if (TryDecode(jsonObjectString, out JsonObject result))
            {
                return result;
            }

            throw new InvalidCastException("Json object string is not well structured!");
        }

        public static bool TryDecode(string jsonObjectString, out JsonObject result)
        {
            var lastIndex = -1;
            if (TryGetNextCharacter(jsonObjectString, ref lastIndex, out char character, true) && character == '{')
            {
                result = DecodeNextJsonObject(jsonObjectString, ref lastIndex);

                return true;
            }

            result = null;

            return false;
        }

        public static bool IsValid(string jsonObjectString)
        {
            if (TryDecode(jsonObjectString, out JsonObject result))
            {
                return true;
            }

            return false;
        }


        public static JsonObject ReadFrom(string fileName)
        {
            var result = (JsonObject)DecodeObject(File.ReadAllText(fileName));
            result.FileName = fileName;

            return result;
        }


        public static JsonObject TryReadFrom(string fileName, JsonObject @default)
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


        public static object GetJsonEquivalent(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JsonObject || value is JsonArray || value is IJsonArray /*|| value is JsonString*/)
            {
                return value;
            }

            if (value is ReadOnlyJsonObject o)
            {
                return new JsonObject(o);
            }

            if (value is ReadOnlyJsonArray array)
            {
                return new JsonArray(array);
            }

            var type = value.GetType();

            if (type.IsValueType || value is string)
            {
                return value;
            }

            if (TryGetKeyValues(value, type, out var keyValues))
            {
                var jsonObject = new JsonObject();

                foreach (var keyValue in keyValues)
                {
                    jsonObject.Add(keyValue.Key, keyValue.Value);
                }

                return jsonObject;
            }

            // IEnumerable<KeyValue<,>> also enters here that is not intended!!!
            if (TryGetEnumerable(value, out var enumerable))
            {
                var jsonArray = new JsonArray();

                var enumerator = enumerable.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    jsonArray.Add(enumerator.Current);
                }

                return jsonArray;
            }

            throw new NotImplementedException($"Can not resolve type '{type.FullName}' to Json convertible type!");
        }

        #endregion

        public static JsonObject ToJsonObject(object value)
        {
            return (JsonObject)GetJsonEquivalent(value);
        }

        #region indexers


        public bool this[string key, bool fallback] => Opt(key, fallback);


        public byte this[string key, byte fallback] => Opt(key, fallback);


        public int this[string key, int fallback] => Opt(key, fallback);


        public long this[string key, long fallback] => Opt(key, fallback);


        public decimal this[string key, decimal fallback] => Opt(key, fallback);


        public float this[string key, float fallback] => Opt(key, fallback);


        public double this[string key, double fallback] => Opt(key, fallback);


        public string this[string key, string fallback] => Opt(key, fallback);


        public DateTime this[string key, DateTime fallback] => Opt(key, fallback);


        public JsonObject this[string key, JsonObject fallback] => Opt(key, fallback);


        public ReadOnlyJsonObject this[string key, ReadOnlyJsonObject fallback] => Opt(key, fallback);


        public JsonArray this[string key, JsonArray fallback] => Opt(key, fallback);


        public ReadOnlyJsonArray this[string key, ReadOnlyJsonArray fallback] => Opt(key, fallback);


        public bool this[string key, bool fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public byte this[string key, byte fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public int this[string key, int fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public long this[string key, long fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public decimal this[string key, decimal fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public float this[string key, float fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public double this[string key, double fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public string this[string key, string fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public DateTime this[string key, DateTime fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public JsonObject this[string key, JsonObject fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);


        public JsonArray this[string key, JsonArray fallback, bool setIfFallbackUsed] => Opt(key, fallback, setIfFallbackUsed);

        #endregion

        #region IDictionary<string, TValue> implementation


        public object this[string key]
        {
            get => GetStorage()[key];
            set
            {
                GetStorage()[key] = DisableJsonDataConvertion ? value : GetJsonEquivalent(value);
            }
        }


        public int Count => GetStorage().Count;


        public bool IsReadOnly => GetStorage().IsReadOnly;


        public ICollection<string> Keys => GetStorage().Keys;


        public ICollection<object> Values => GetStorage().Values;


        public void Add(KeyValuePair<string, object> item)
        {
            Add(item.Key, item.Value);
        }


        public void Add(string key, object value)
        {
            GetStorage().Add(key, DisableJsonDataConvertion ? value : GetJsonEquivalent(value));
        }


        public void Clear()
        {
            GetStorage().Clear();
        }


        public bool Contains(KeyValuePair<string, object> item)
        {
            return GetStorage().Contains(item);
        }


        public bool ContainsKey(string key)
        {
            return GetStorage().ContainsKey(key);
        }


        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            GetStorage().CopyTo(array, arrayIndex);
        }


        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return GetStorage().GetEnumerator();
        }


        public bool Remove(KeyValuePair<string, object> item)
        {
            return GetStorage().Remove(item);
        }


        public bool Remove(string key)
        {
            return GetStorage().Remove(key);
        }


        public bool TryGetValue(string key, out object value)
        {
            return GetStorage().TryGetValue(key, out value);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region public override methods


        public override string ToString()
        {
            return ToString(Encoding);
        }

        #endregion

        #region public methods


        public string ToString(EncodingBases encodingModes, bool safeTypeReversible)
        {
            return EncodeJsonObject(this, encodingModes, safeTypeReversible);
        }

        public string ToString(EncodingBases encodingModes)
        {
            return ToString(encodingModes, false);
        }

        public string ToString(bool safeTypeReversible)
        {
            return ToString(EncodingBases.Default, safeTypeReversible);
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


        /// <summary>
        /// Returns the value mapped by name, or throws if no such mapping exists.
        /// </summary>
        /// <param name="key">Name of key to get the value mapped to it.</param>
        /// <returns>Returns an object that is mapped to the associated key.</returns>
        /// <exception cref="KeyNotFoundException" />
        public object Get(string key)
        {
            return this[key];
        }


        /// <summary>
        /// Returns the value mapped by <paramref name="key"/> if its type is <typeparamref name="TType"/>, or throws if no such mapping exists.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="key">Name of key to get the value mapped to it.</param>
        /// <returns>Returns an object of type <typeparamref name="TType"/> that is mapped to the associated key</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException">No mapped value to <paramref name="key"/> is found.</exception>
        /// <exception cref="InvalidCastException">It is not possible to cast value mapped to <paramref name="key"/> to type <typeparamref name="TType"/>.</exception>
        public TType Get<TType>(string key)
        {
            return (TType)this[key];
        }


        public bool GetBoolean(string key)
        {
            return Get<bool>(key);
        }


        public byte GetInt16(string key)
        {
            return Get<byte>(key);
        }


        public int GetInt32(string key)
        {
            return Get<int>(key);
        }


        public long GetInt64(string key)
        {
            return Get<long>(key);
        }


        public decimal GetDecimal(string key)
        {
            return Get<decimal>(key);
        }


        public float GetSingle(string key)
        {
            return Get<float>(key);
        }


        public double GetDouble(string key)
        {
            return Get<double>(key);
        }


        public string GetString(string key)
        {
            return Get<string>(key);
        }


        public DateTime GetDateTime(string key)
        {
            return Get<DateTime>(key);
        }


        public JsonObject GetJsonObject(string key)
        {
            return Get<JsonObject>(key);
        }


        public ReadOnlyJsonObject GetReadOnlyJsonObject(string key)
        {
            return Get<ReadOnlyJsonObject>(key);
        }


        public JsonArray GetJsonArray(string key)
        {
            return Get<JsonArray>(key);
        }

        public JsonArray<TType> GetJsonArray<TType>(string key)
        {
            var jsonArray = Get<IJsonArray>(key);

            if (jsonArray is JsonArray<TType> a)
                return a;

            return ((JsonArray)jsonArray).AsJsonArray<TType>();
        }


        public ReadOnlyJsonArray GetReadOnlyJsonArray(string key)
        {
            return Get<ReadOnlyJsonArray>(key);
        }



        public bool IsNull(string key)
        {
            return !ContainsKey(key) || this[key] == null;
        }



        public object Opt(string key)
        {
            return ContainsKey(key) ? this[key] : null;
        }


        // TODO: There is a HUGE Trap here. If fallback is set but its equivalent is stored then first return would be diffrent from next returns!
        public TType Opt<TType>(string key, TType fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            if (ContainsKey(key))
            {
                var result = this[key];

                if (result == null)
                {
                    if (setIfFallbackUsed) { this[key] = fallback; }

                    isFallbackReturned = true;

                    return fallback;
                }

                if (result is TType type)
                {
                    isFallbackReturned = false;

                    return type;
                }

                Type resultType = null;
                Type destinationType = null;

                if (result is Enum && !(destinationType = typeof(TType)).IsEnum)
                {
                    resultType = Enum.GetUnderlyingType(result.GetType());

                    result = Convert.ChangeType(result, resultType);

                    if (result is TType type2)
                    {
                        isFallbackReturned = false;

                        return type2;
                    }
                }

                destinationType = destinationType ?? typeof(TType);

                if (result is JsonArray jsonArrayResult)
                {
                    if (destinationType.IsGenericType && destinationType.GenericTypeArguments.Length == 1 &&
                        destinationType.GetGenericTypeDefinition() == typeof(JsonArray<>))
                    {
                        isFallbackReturned = false;

                        var method = typeof(JsonArray).GetMethod("AsJsonArray");
                        var generic = method.MakeGenericMethod(destinationType.GenericTypeArguments.Single());

                        return (TType)generic.Invoke(jsonArrayResult, null);
                    }
                }

                resultType = resultType ?? result.GetType();

                if (destinationType.IsValueType && resultType.IsValueType)
                {
                    try
                    {
                        isFallbackReturned = false;

                        return (TType)Convert.ChangeType(result, typeof(TType));
                    }
                    catch (Exception)
                    {
                        if (setIfFallbackUsed) { this[key] = fallback; }

                        isFallbackReturned = true;

                        return fallback;
                    }
                }

                if (destinationType.IsValueType)
                {
                    try
                    {
                        isFallbackReturned = false;

                        if (destinationType == typeof(bool)) { return (TType)(object)Convert.ToBoolean(result); }
                        if (destinationType == typeof(byte)) { return (TType)(object)Convert.ToByte(result); }
                        if (destinationType == typeof(char)) { return (TType)(object)Convert.ToChar(result); }
                        if (destinationType == typeof(DateTime))
                        {
                            if (result is string s) { return (TType)(object)DateTime.Parse(s, EnglishUsCultureInfo, DateTimeStyles.None); }

                            return (TType)(object)Convert.ToDateTime(result);
                        }
                        if (destinationType == typeof(decimal)) { return (TType)(object)Convert.ToDecimal(result); }
                        if (destinationType == typeof(double)) { return (TType)(object)Convert.ToDouble(result); }
                        if (destinationType == typeof(short)) { return (TType)(object)Convert.ToInt16(result); }
                        if (destinationType == typeof(int)) { return (TType)(object)Convert.ToInt32(result); }
                        if (destinationType == typeof(long)) { return (TType)(object)Convert.ToInt64(result); }
                        if (destinationType == typeof(sbyte)) { return (TType)(object)Convert.ToSByte(result); }
                        if (destinationType == typeof(float)) { return (TType)(object)Convert.ToSingle(result); }
                        if (destinationType == typeof(ushort)) { return (TType)(object)Convert.ToUInt16(result); }
                        if (destinationType == typeof(uint)) { return (TType)(object)Convert.ToUInt32(result); }
                        if (destinationType == typeof(ulong)) { return (TType)(object)Convert.ToUInt64(result); }
                    }
                    catch (Exception)
                    {
                        if (setIfFallbackUsed) { this[key] = fallback; }

                        isFallbackReturned = true;

                        return fallback;
                    }
                }
                else if (destinationType == typeof(string))
                {
                    isFallbackReturned = false;

                    return (TType)(object)Convert.ToString(result);
                }

                if (setIfFallbackUsed) { this[key] = fallback; }

                isFallbackReturned = true;

                return fallback;
            }

            if (setIfFallbackUsed) { this[key] = fallback; }

            isFallbackReturned = true;

            return fallback;
        }


        public TType Opt<TType>(string key, TType fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed, out _);
        }


        public TType Opt<TType>(string key, TType fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, false, out isFallbackReturned);
        }


        public TType Opt<TType>(string key, TType fallback)
        {
            return Opt(key, fallback, false);
        }


        public TType Opt<TType>(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(TType), false, out isFallbackReturned);
        }


        public TType Opt<TType>(string key)
        {
            return Opt(key, default(TType));
        }



        public bool OptBoolean(string key, bool fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public bool OptBoolean(string key, bool fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public bool OptBoolean(string key, bool fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public bool OptBoolean(string key, bool fallback)
        {
            return Opt(key, fallback);
        }


        public bool OptBoolean(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(bool), out isFallbackReturned);
        }


        public bool OptBoolean(string key)
        {
            return Opt<bool>(key);
        }



        public byte OptInt16(string key, byte fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public byte OptInt16(string key, byte fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public byte OptInt16(string key, byte fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public byte OptInt16(string key, byte fallback)
        {
            return Opt(key, fallback);
        }


        public byte OptInt16(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(byte), out isFallbackReturned);
        }


        public byte OptInt16(string key)
        {
            return Opt<byte>(key);
        }



        public int OptInt32(string key, int fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public int OptInt32(string key, int fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public int OptInt32(string key, int fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public int OptInt32(string key, int fallback)
        {
            return Opt(key, fallback);
        }


        public int OptInt32(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(int), out isFallbackReturned);
        }


        public int OptInt32(string key)
        {
            return Opt<int>(key);
        }



        public long OptInt64(string key, long fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public long OptInt64(string key, long fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public long OptInt64(string key, long fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public long OptInt64(string key, long fallback)
        {
            return Opt(key, fallback);
        }


        public long OptInt64(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(long), out isFallbackReturned);
        }


        public long OptInt64(string key)
        {
            return Opt<long>(key);
        }



        public decimal OptDecimal(string key, decimal fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public decimal OptDecimal(string key, decimal fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public decimal OptDecimal(string key, decimal fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public decimal OptDecimal(string key, decimal fallback)
        {
            return Opt(key, fallback);
        }


        public decimal OptDecimal(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(decimal), out isFallbackReturned);
        }


        public decimal OptDecimal(string key)
        {
            return Opt<decimal>(key);
        }



        public float OptSingle(string key, float fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public float OptSingle(string key, float fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public float OptSingle(string key, float fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public float OptSingle(string key, float fallback)
        {
            return Opt(key, fallback);
        }


        public float OptSingle(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(float), out isFallbackReturned);
        }


        public float OptSingle(string key)
        {
            return Opt<float>(key);
        }



        public double OptDouble(string key, double fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public double OptDouble(string key, double fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public double OptDouble(string key, double fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public double OptDouble(string key, double fallback)
        {
            return Opt(key, fallback);
        }


        public double OptDouble(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(double), out isFallbackReturned);
        }


        public double OptDouble(string key)
        {
            return Opt<double>(key);
        }



        public string OptString(string key, string fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public string OptString(string key, string fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public string OptString(string key, string fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public string OptString(string key, string fallback)
        {
            return Opt(key, fallback);
        }


        public string OptString(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(string), out isFallbackReturned);
        }


        public string OptString(string key)
        {
            return Opt<string>(key);
        }



        public DateTime OptDateTime(string key, DateTime fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public DateTime OptDateTime(string key, DateTime fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public DateTime OptDateTime(string key, DateTime fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public DateTime OptDateTime(string key, DateTime fallback)
        {
            return Opt(key, fallback);
        }


        public DateTime OptDateTime(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(DateTime), out isFallbackReturned);
        }


        public DateTime OptDateTime(string key)
        {
            return Opt<DateTime>(key);
        }



        public JsonObject OptJsonObject(string key, JsonObject fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public JsonObject OptJsonObject(string key, JsonObject fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public JsonObject OptJsonObject(string key, JsonObject fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public JsonObject OptJsonObject(string key, JsonObject fallback)
        {
            return Opt(key, fallback);
        }


        public JsonObject OptJsonObject(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(JsonObject), out isFallbackReturned);
        }


        public JsonObject OptJsonObject(string key)
        {
            return Opt<JsonObject>(key);
        }



        public JsonArray OptJsonArray(string key, JsonArray fallback, bool setIfFallbackUsed, out bool isFallbackReturned)
        {
            return Opt(key, fallback, setIfFallbackUsed, out isFallbackReturned);
        }


        public JsonArray OptJsonArray(string key, JsonArray fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }

        public JsonArray<TType> OptJsonArray<TType>(string key, JsonArray<TType> fallback, bool setIfFallbackUsed)
        {
            return Opt(key, fallback, setIfFallbackUsed);
        }


        public JsonArray OptJsonArray(string key, JsonArray fallback, out bool isFallbackReturned)
        {
            return Opt(key, fallback, out isFallbackReturned);
        }


        public JsonArray OptJsonArray(string key, JsonArray fallback)
        {
            return Opt(key, fallback);
        }


        public JsonArray OptJsonArray(string key, out bool isFallbackReturned)
        {
            return Opt(key, default(JsonArray), out isFallbackReturned);
        }


        public JsonArray OptJsonArray(string key)
        {
            return Opt<JsonArray>(key);
        }


        public ExpandoObject ToExpandoObject()
        {
            var expandoObject = (IDictionary<string, object>)new ExpandoObject();

            foreach (var item in this)
            {
                var value = item.Value;

                if (value is JsonObject) { value = ((JsonObject)value).ToExpandoObject(); }
                else if (value is JsonArray) { value = ToArray((JsonArray)value); }

                expandoObject.Add(item.Key, value);
            }

            return (ExpandoObject)expandoObject;
        }

        #endregion
    }
}
