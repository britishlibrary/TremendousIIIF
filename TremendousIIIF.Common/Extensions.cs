using System;
using System.Linq;

namespace TremendousIIIF.Common
{
    public static class Extensions
    {
        public static TAttribute GetAttribute<TAttribute>(this Enum value)
    where TAttribute : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            return type.GetField(name)
                .GetCustomAttributes(false)
                .OfType<TAttribute>()
                .SingleOrDefault();
        }

        public static string GetError(this ArgumentException ex)
        {
            if (ex is null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            if (ex.ParamName == null)
            {
                return ex.Message;
            }
            var idx = ex.Message.LastIndexOf(Environment.NewLine);
            return idx != -1 ? ex.Message.Remove(idx) : ex.Message;
        }
    }
}
