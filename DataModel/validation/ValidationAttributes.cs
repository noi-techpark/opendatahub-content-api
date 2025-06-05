// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataModel.validation
{
    public class UrlPrefixAttribute : ValidationAttribute
    {        
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            //If empty Url is passed do not validate
            if (value?.ToString() == null || String.IsNullOrEmpty(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var urlRegex = new Regex(
            @"^(https?|ftps?):\/\/(?:[a-zA-Z0-9]" +
                    @"(?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}" +
                    @"(?::(?:0|[1-9]\d{0,3}|[1-5]\d{4}|6[0-4]\d{3}" +
                    @"|65[0-4]\d{2}|655[0-2]\d|6553[0-5]))?" +
                    @"(?:\/(?:[-a-zA-Z0-9@%_\+.~#?&=]+\/?)*)?$",
            RegexOptions.IgnoreCase);
            urlRegex.Matches(value.ToString());

            if (urlRegex.IsMatch(value.ToString()))
            {
                return ValidationResult.Success;
            }

            var msg = $"Please enter a valid Url";
            return new ValidationResult(msg);
        }
    }
}
