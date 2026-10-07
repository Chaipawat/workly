using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workly.Api;

public sealed class AuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var anonymous = method.IsDefined(typeof(AllowAnonymousAttribute))
            || method.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute)) == true;
        var authorized = method.IsDefined(typeof(AuthorizeAttribute))
            || method.DeclaringType?.IsDefined(typeof(AuthorizeAttribute)) == true;
        if (anonymous || !authorized) operation.Security = [];
    }
}
