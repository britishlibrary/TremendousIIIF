using System;
using System.Diagnostics.CodeAnalysis;

namespace TremendousIIIF.Common
{
    public enum ApiVersion
    {
        [ApiSpecification("https://iiif.io/api/image/2.1/")]
        [ContextUri("http://iiif.io/api/image/2/context.json")]
        [ProfileUri("http://iiif.io/api/image/2/level2.json")]
        v2_1 = 0,
        [ApiSpecification("https://iiif.io/api/image/3.0/")]
        [ContextUri("http://iiif.io/api/image/3/context.json")]
        [ProfileUri("http://iiif.io/api/image/3/level2.json")]
        v3_0 = 1
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class ApiSpecificationAttribute : Attribute
    {
        public string SpecificationUri { get; private set; }


        public ApiSpecificationAttribute(string specUri)
        {
            SpecificationUri = specUri;
        }

    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class ContextUriAttribute : Attribute
    {
        public string ContextUri { get; private set; }


        public ContextUriAttribute(string contextUri)
        {
            ContextUri = contextUri;
        }

    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class ProfileUriAttribute : Attribute
    {
        public string ProfileUri { get; private set; }


        public ProfileUriAttribute(string profileUri)
        {
            ProfileUri = profileUri;
        }

    }
}
