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
            var sb = new StringBuilder(Name);
            if (GenericArgs?.Length > 0)
            {
                sb.Append("[[");
                sb.Append(GenericArgs[0].ToString());
                sb.Append(']');
                for (var i = 1; i < GenericArgs.Length; i++)
                {
                    sb.Append(",[");
                    sb.Append(GenericArgs[i].ToString());
                }
                sb.Append(']');
            }
            if (Assembly != null)
                sb.Append(", ").Append(Assembly.ToString());
            return sb.ToString();
        }
    }
}
