// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Helper.Generic
{
    /// <summary>
    /// Error responses in the ValidationProblemDetails format ({ title, status, errors }),
    /// the databrowser shows the errors entries directly to the user
    /// </summary>
    public static class CustomApiActionResults
    {
        public static IActionResult ForbiddenValidation(string message)
        {
            return new ObjectResult(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]> { { "Authorization", new[] { message } } }
                )
                {
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                }
            )
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
        }

        public static IActionResult BadRequestValidation(string message)
        {
            return new ObjectResult(
                new ValidationProblemDetails(
                    new Dictionary<string, string[]> { { "Error", new[] { message } } }
                )
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                }
            )
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }
    }
}
