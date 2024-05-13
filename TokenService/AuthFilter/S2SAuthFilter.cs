namespace TokenService.AuthFilter
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using System;
    using TokenService.Config;

    // IOperationFilter is for swagger documentation
    public class S2SAuthFilter : IActionFilter, IOperationFilter
    {
        private readonly TokenServiceConfig _tokenServiceConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public S2SAuthFilter(TokenServiceConfig tokenServiceConfig, IHttpContextAccessor httpContextAccessor)
        {
            _tokenServiceConfig = tokenServiceConfig ?? throw new ArgumentNullException(nameof(tokenServiceConfig));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            string s2sKey = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];

            if (string.IsNullOrWhiteSpace(s2sKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            string expectedS2SKey = GetExpectedS2SKey();

            if (!string.Equals(s2sKey, expectedS2SKey, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedResult();
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Implementation if needed, otherwise leave empty  
        }

        private string GetExpectedS2SKey()
        {
            if (_tokenServiceConfig.UseEnvironmentVariablesFirst)
            {
                return Environment.GetEnvironmentVariable("S2S_KEY") ?? _tokenServiceConfig.S2S_KEY;
            }

            return _tokenServiceConfig.S2S_KEY;
        }

        // for swagger documentation
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasS2SAuthFilter = context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<S2SAuthFilter>().Any()
                                   || context.MethodInfo.GetCustomAttributes(true).OfType<S2SAuthFilter>().Any();
            if (hasS2SAuthFilter)
                operation.Security = new List<OpenApiSecurityRequirement> {
                    new OpenApiSecurityRequirement {
                        [
                            new OpenApiSecurityScheme {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "S2SAuth" }
                            }
                        ] = Array.Empty<string>()
                    }
                };
        }
    }

}
