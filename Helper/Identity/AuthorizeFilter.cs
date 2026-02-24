// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
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
        // 1. New Constructor for LIST/ARRAY usage (Additional Usage)
        // This allows: [AuthorizeODH(new[] {PermissionAction.Create, PermissionAction.Update})]
        // or: [AuthorizeODH(actions: new[] {PermissionAction.Create, PermissionAction.Update})]
        public AuthorizeODHAttribute(PermissionAction[] actions)
            : base(typeof(AuthorizeODHActionFilter))
        {
            // Pass the array of actions to the filter
            Arguments = new object[] { actions.ToList() };
        }

        // 2. Retrocompatible Constructor for SINGLE action usage (Legacy Usage)
        // This allows: [AuthorizeODH(PermissionAction.Create)]
        public AuthorizeODHAttribute(PermissionAction action)
            : base(typeof(AuthorizeODHActionFilter))
        {
            // Convert the single action into a List<PermissionAction> 
            // to maintain a consistent argument type for the filter.
            Arguments = new object[] { new List<PermissionAction> { action } };
        }
    }

    public class AuthorizeODHActionFilter : IAuthorizationFilter
    {
        private readonly List<PermissionAction> _actions;

        public AuthorizeODHActionFilter(List<PermissionAction> actions)
        {
            _actions = actions;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            bool isAuthorized = CheckAccess(
                context.HttpContext.User,
                _actions,
                context.HttpContext.Request.Path.GetPathNextToCombinedRoutes("/", "v1")
            ); // :)

            if (!isAuthorized)
            {
                context.Result = new ForbidResult();
            }
        }

        private bool CheckAccess(ClaimsPrincipal User, List<PermissionAction> actions, string endpoint)
        {
            // If the endpoint is null or empty, access is denied.
            if (String.IsNullOrEmpty(endpoint))
                return false;

            // If the list of actions is null or empty, access is allowed by default 
            // (though you might want to adjust this based on security policy, e.g., return false).
            if (actions == null || actions.Count == 0)
                return true; 

            // The All() method checks if ALL elements in the 'actions' list satisfy the condition.
            return actions.All(action =>
                // For each action, we check if the User has a claim that matches the required format:
                // "endpoint_ACTION"
                User.Claims.Any(c =>
                    c.Type == ClaimTypes.Role
                    // Constructs the required role string, e.g., "products_Read"
                    // StartsWith is needed because some Claims are inserted as ex. article_create_source=noi
                    && c.Value.StartsWith(endpoint + "_" + action.ToString(), StringComparison.OrdinalIgnoreCase)
                )
            );
        }
    }
}
