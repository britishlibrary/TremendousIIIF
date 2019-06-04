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
    => ex.ParamName == null ?
        ex.Message : ex.Message.Remove(ex.Message.LastIndexOf(Environment.NewLine));
    }
}
