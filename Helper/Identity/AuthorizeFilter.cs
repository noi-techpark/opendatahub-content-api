﻿// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Helper.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Helper.Identity
{
    public enum PermissionAction
    {
        Read,
        Create,
        Update,
        Delete,
    }

    public class AuthorizeODHAttribute : TypeFilterAttribute
    {
        public AuthorizeODHAttribute(PermissionAction action)
            : base(typeof(AuthorizeODHActionFilter))
        {
            Arguments = new object[] { action };
        }
    }

    public class AuthorizeODHActionFilter : IAuthorizationFilter
    {
        private readonly PermissionAction _action;

        public AuthorizeODHActionFilter(PermissionAction action)
        {
            _action = action;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool isAuthorized = CheckAccess(
                context.HttpContext.User,
                _action,
                context.HttpContext.Request.Path.GetPathNextTo("/", "v1")
            ); // :)

            if (!isAuthorized)
            {
                context.Result = new ForbidResult();
            }
        }

        private bool CheckAccess(ClaimsPrincipal User, PermissionAction action, string endpoint)
        {
            if (!String.IsNullOrEmpty(endpoint))
                return User.Claims.Any(c =>
                    c.Type == ClaimTypes.Role
                    && c.Value.StartsWith(endpoint + "_" + action.ToString())
                );
            else
                return false;
        }
    }
}
