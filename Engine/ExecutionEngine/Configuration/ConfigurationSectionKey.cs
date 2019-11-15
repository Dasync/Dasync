using System;
using System.Text;

namespace Dasync.ExecutionEngine.Configuration
{
    public struct ConfigurationSectionKey
    {
        public ServiceCategory ServiceCategory { get; set; }

        public PrimitiveType PrimitiveType { get; set; }

        public string ServiceName { get; set; }

        public string PrimitiveName { get; set; }

        public string SectionName { get; set; }

        public static bool operator ==(ConfigurationSectionKey key1, ConfigurationSectionKey key2)
        {
            return
                key1.ServiceCategory == key2.ServiceCategory &&
                key1.PrimitiveType == key2.PrimitiveType &&
                key1.ServiceName == key2.ServiceName &&
                key1.PrimitiveName == key2.PrimitiveName &&
                key1.SectionName == key2.SectionName;
        }

        public static bool operator !=(ConfigurationSectionKey key1, ConfigurationSectionKey key2) => (key1 == key2);

        public override bool Equals(object obj) => (obj is ConfigurationSectionKey key) && this == key;

        public override int GetHashCode()
        {
            int code = 0;

            code = code * 104651 + (int)ServiceCategory;
            code = code * 104651 + ((int)PrimitiveType + 11);

            if (ServiceName != null)
                code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(ServiceName);

            if (PrimitiveName != null)
                code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(PrimitiveName);

            if (SectionName != null)
                code = code * 104651 + StringComparer.OrdinalIgnoreCase.GetHashCode(SectionName);

            return code;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            if (!string.IsNullOrEmpty(ServiceName))
            {
                result.Append("services:").Append(ServiceName);
            }
            else if (ServiceCategory == ServiceCategory.Local)
            {
                result.Append("services:_local");
            }
            else if (ServiceCategory == ServiceCategory.External)
            {
                result.Append("services:_external");
            }

            switch (PrimitiveType)
            {
                case PrimitiveType.Command:
                    if (result.Length > 0)
                        result.Append(':');
                    result.Append("commands");
                    break;

                case PrimitiveType.Query:
                    if (result.Length > 0)
                        result.Append(':');
                    result.Append("queries");
                    break;

                case PrimitiveType.Event:
                    if (result.Length > 0)
                        result.Append(':');
                    result.Append("events");
                    break;
            }

            if (!string.IsNullOrEmpty(PrimitiveName))
            {
                result.Append(":").Append(PrimitiveName);
            }
            else if (PrimitiveType != PrimitiveType.Any && ServiceName != null)
            {
                result.Append(":_all");
            }

            if (!string.IsNullOrEmpty(SectionName))
            {
                result.Append(":").Append(SectionName);
            }

            return result.ToString();
        }
    }
}
