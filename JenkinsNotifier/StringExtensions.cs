using System;

namespace JenkinsNotifier
{
    internal static class StringExtensions
    {
        public static T To<T>(this string value)
        {
            Type nullableType = typeof (T);
            Type underlyingType = Nullable.GetUnderlyingType(nullableType);
            bool flag = underlyingType != (Type) null;
            if (string.IsNullOrEmpty(value))
                return default(T);
            try
            {
                return (T) value.Cast(flag ? underlyingType : nullableType);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Failed to convert value '{0}' to type '{1}'!", (object) value, (object) nullableType.Name), ex);
            }
        }

        private static object Cast(this string value, Type type)
        {
            if (type.IsEnum)
                return Enum.Parse(type, value);
            return type == typeof (Guid) ? (object) Guid.Parse(value) : Convert.ChangeType((object) value, type);
        }
    }
}