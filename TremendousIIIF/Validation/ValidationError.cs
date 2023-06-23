using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TremendousIIIF.Common;

namespace TremendousIIIF.Validation
{
    public readonly struct ValidationError
    {
        public ValidationError(string error, bool includeSpecLink = true)
        {
            Error = error;
            IncludeSpecLink = includeSpecLink;
            SpecRegion = string.Empty;
        }

        public ValidationError(string error, string specRegion)
        {
            Error = error;
            IncludeSpecLink = true;
            SpecRegion = specRegion;
        }

        public string Error { get; }
        public bool IncludeSpecLink { get; }
        public string SpecRegion { get; }
    }

    public static class ValidationExtensions
    {
        public static ProblemDetails ToProblemDetail(this ValidationError self, ApiVersion apiVersion)
        {
            var problem = new ProblemDetails
            {
                Title = "Invalid IIIF Image API request",
                Detail = self.Error
            };
            if (self.IncludeSpecLink)
            {
                var specUri = GetSpecUri(apiVersion, self.SpecRegion);
                problem.Detail += $". Please consult the specification for details: {specUri}";
                problem.Instance = specUri;
            }

            return problem;
        }

        private static string GetSpecUri(ApiVersion apiVersion, string specRegion)
        {
            return $"{apiVersion.GetAttribute<ApiSpecificationAttribute>().SpecificationUri}#{specRegion}";
        }

    }
}