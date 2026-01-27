using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public static class CustomApiActionResults
    {
        public static IActionResult ForbiddenValidation(string message)
        {
            return new ObjectResult(
                new ValidationProblemDetails(new Dictionary<string, string[]>
                {
            { "Authorization", new[] { message } }
                })
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
                })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        public static IActionResult BadRequestValidation(string message)
        {
            return new ObjectResult(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]> {  { "Error", new[] { message } }  }
                    )
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1"
                })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}
