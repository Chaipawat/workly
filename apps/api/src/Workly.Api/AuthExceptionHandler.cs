using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Workly.Application.Auth;

namespace Workly.Api;

public sealed class AuthExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AuthException authException) return false;
        context.Response.StatusCode = authException.StatusCode;
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = authException.StatusCode,
                Title = authException.Message
            }
        });
        return true;
    }
}
