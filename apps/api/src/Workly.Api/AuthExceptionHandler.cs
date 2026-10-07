using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Workly.Application.Auth;
using Workly.Application.Workspace;

namespace Workly.Api;

public sealed class AuthExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            AuthException authException => (authException.StatusCode, authException.Message),
            WorkspaceException workspaceException => (workspaceException.StatusCode, workspaceException.Message),
            _ => (0, "")
        };
        if (statusCode == 0) return false;
        context.Response.StatusCode = statusCode;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = message
            }
        });
        return true;
    }
}
