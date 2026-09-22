// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OdhApiCore.Controllers
{
    [ModelBinder(typeof(PageSizeBinder))]
    public class PageSize
    {
        public int? Value { get; }

        /// <summary>
        /// True if "pagesize" was actually present in the request (any value, including "null"/"-1"/"0"),
        /// false if the query string didn't contain it at all and Value was defaulted by the binder.
        /// </summary>
        public bool WasExplicitlySet { get; }

        public PageSize(int? value, bool wasExplicitlySet = true)
        {
            this.Value = value;
            this.WasExplicitlySet = wasExplicitlySet;
        }

        public static implicit operator int?(PageSize pagesize)
        {
            return pagesize.Value;
        }

        public static implicit operator PageSize(int value)
        {
            return new PageSize(value);
        }

        public static implicit operator PageSize(int? value)
        {
            return new PageSize(value);
        }
    }

    public class PageSizeBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(
                bindingContext.ModelName
            );
            var firstValue = valueProviderResult.FirstValue;
            if (firstValue == null) // genuinely missing from the request
            {
                bindingContext.Result = ModelBindingResult.Success(
                    new PageSize(10, wasExplicitlySet: false)
                );
            }
            else if (firstValue == "null") // "null" exists for compatibility reasons
            {
                bindingContext.Result = ModelBindingResult.Success(new PageSize(10));
            }
            else if (firstValue == "-1" || firstValue == "0")
            {
                bindingContext.Result = ModelBindingResult.Success(new PageSize(int.MaxValue));
            }
            else if (int.TryParse(firstValue, out var value))
            {
                bindingContext.Result = ModelBindingResult.Success(new PageSize(value));
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.ValueIsInvalidAccessor(
                        firstValue
                    )
                );
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }
    }
}
