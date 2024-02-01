﻿// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Identity
{
    public enum PermissionAction
    {
        Read,
        Create,
        Update,
        Delete
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
            bool isAuthorized = CheckAccess(context.HttpContext.User, _action, context.HttpContext.Request.Path); // :)

            if (!isAuthorized)
            {
                context.Result = new ForbidResult();
            }
        }

        private bool CheckAccess(ClaimsPrincipal User, PermissionAction action, string endpoint)
        {
          
            var lastendpointelement = endpoint.Split('/').Last();

            //TODO on DELETE + UPDATE the Id is passed in the route....
            if (action == PermissionAction.Delete || action == PermissionAction.Update)
            {
                lastendpointelement = endpoint.Split('/')[endpoint.Split('/').Length - 1];
            }


            return User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value.StartsWith(lastendpointelement + "_" + action.ToString()));
        }
    }    
}
