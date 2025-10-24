// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public static class BoolExtensions
    {
        public static bool TrySetBool(this ref bool target, string? value)
        {
            if (bool.TryParse(value, out var result))
            {
                target = result;
                return true;
            }
            return false;
        }
    }
}
