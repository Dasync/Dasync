using System;
using System.Collections.Generic;
using System.Text;

namespace Dasync.Serialization
{
    public sealed class TypeSerializationInfo
    {
        public string Name;
        public AssemblySerializationInfo Assembly;
        public TypeSerializationInfo[] GenericArgs;

        public override bool Equals(object obj)
        {
            if (obj is TypeSerializationInfo info)
                return this == info;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashcode = 0;
                if (Name != null)
                    hashcode = Name.GetHashCode();
                if (Assembly != null)
                    hashcode = hashcode * 104651 + Assembly.GetHashCode();
                if (GenericArgs?.Length > 0)
                {
                    foreach (var genericArgType in GenericArgs)
                        hashcode = hashcode * 104651 + genericArgType.GetHashCode();
                }
                return hashcode;
            }
        }

        public static bool operator ==(TypeSerializationInfo info1, TypeSerializationInfo info2)
        {
            if (info1 is null && info2 is null)
                return true;

            if (info1 is null || info2 is null)
                return false;

            if (info1.Name != info2.Name)
                return false;

            if (info1.Assembly != info2.Assembly)
                return false;

            if ((info1.GenericArgs?.Length ?? 0) != (info2.GenericArgs?.Length ?? 0))
                return false;

            if (info1.GenericArgs != null && info2.GenericArgs != null)
            {
                for (var i = 0; i < info1.GenericArgs.Length; i++)
                {
                    var genericType1 = info1.GenericArgs[i];
                    var genericType2 = info2.GenericArgs[i];
                    if (genericType1 != genericType2)
                        return false;
                }
            }

            return true;
        }

        public static bool operator !=(TypeSerializationInfo info1, TypeSerializationInfo info2)
            => !(info1 == info2);

        public static readonly TypeSerializationInfo Self = TypeExtensions.CreateTypeSerializationInfo(typeof(TypeSerializationInfo));

        public override string ToString()
        {
            var sb = new StringBuilder();
            ToStringInternal(sb);
            return sb.ToString();
        }

        internal void ToStringInternal(StringBuilder sb)
        {
            sb.Append(Name);
            if (GenericArgs?.Length > 0)
            {
                sb.Append("[");

                for (var i = 0; i < GenericArgs.Length; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    if (GenericArgs[i].Assembly != null)
                        sb.Append("[");

                    GenericArgs[i].ToStringInternal(sb);

                    if (GenericArgs[i].Assembly != null)
                        sb.Append(']');
                }
                sb.Append(']');
            }
            if (Assembly != null)
            {
                sb.Append(", ");
                Assembly.ToStringInternal(sb);
            }
        }

        public static TypeSerializationInfo Parse(string typeFullName)
        {
            var index = 0;
            return ParseInternal(typeFullName, ref index);
        }

        internal static TypeSerializationInfo ParseInternal(string typeFullName, ref int startIndex)
        {
            var result = new TypeSerializationInfo();

            // Skip whitespace
            while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

            // Parse Name
            var nameEndIndex = startIndex;
            for ( ; nameEndIndex < typeFullName.Length; nameEndIndex++)
            {
                if (typeFullName[nameEndIndex] == '[' ||
                    typeFullName[nameEndIndex] == ']' ||
                    typeFullName[nameEndIndex] == ',' ||
                    typeFullName[nameEndIndex] == ' ')
                    break;
            }
            result.Name = typeFullName.Substring(startIndex, nameEndIndex - startIndex);
            startIndex = nameEndIndex;

            // Skip whitespace
            while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

            // Generic arguments
            if (startIndex < typeFullName.Length && typeFullName[startIndex] == '[')
            {
                startIndex++;

                // Skip whitespace
                while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

                var genericArgs = new List<TypeSerializationInfo>(capacity: 2);

                var hasMoreGenericArgs = true;

                while (hasMoreGenericArgs)
                {
                    bool isTypeNameQuoted = false;
                    if (typeFullName[startIndex] == '[')
                    {
                        isTypeNameQuoted = true;
                        startIndex++;
                    }

                    // Skip whitespace
                    while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

                    var genericArg = ParseInternal(typeFullName, ref startIndex);
                    genericArgs.Add(genericArg);

                    // Skip whitespace
                    while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

                    if (isTypeNameQuoted)
                    {
                        if (typeFullName[startIndex] != ']')
                            throw new InvalidOperationException("Type full name format error");
                        startIndex++;

                        // Skip whitespace
                        while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;
                    }

                    if (typeFullName[startIndex] == ']')
                    {
                        hasMoreGenericArgs = false;

                        startIndex++;

                        // Skip whitespace
                        while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;
                    }
                    else if (typeFullName[startIndex] == ',')
                    {
                        startIndex++;

                        // Skip whitespace
                        while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;
                    }
                    else
                    {
                        throw new InvalidOperationException("Type full name format error");
                    }
                }

                result.GenericArgs = genericArgs.ToArray();
            }

            // Assembly name
            if (startIndex < typeFullName.Length && typeFullName[startIndex] == ',')
            {
                startIndex++;

                // Skip whitespace
                while (startIndex < typeFullName.Length && typeFullName[startIndex] == ' ') startIndex++;

                var endIndex = typeFullName.IndexOf(']', startIndex);
                if (endIndex < 0)
                    endIndex = typeFullName.Length;

                result.Assembly = AssemblySerializationInfo.ParseInternal(typeFullName, startIndex, endIndex);
                startIndex = endIndex;
            }

            return result;
        }
    }
}
