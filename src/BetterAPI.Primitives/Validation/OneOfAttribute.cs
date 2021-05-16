// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace BetterAPI.Validation
{
    public sealed class OneOfAttribute : ValidationAttribute
    {
        private readonly object[] _oneOf;

        public OneOfAttribute(params object[] oneOf)
        {
            _oneOf = oneOf.OrderBy(x => x).ToArray();
            OneOfStrings = oneOf.Select(x => x.ToString() ?? string.Empty).ToArray();
            ErrorMessage = "{0} is not one of: {1}";
        }

        public string[] OneOfStrings { get; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            return _oneOf.Contains(value)
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(value?.ToString()));
        }

        public override string FormatErrorMessage(string? name)
        {
            return string.Format(CultureInfo.CurrentCulture, ErrorMessageString,
                new object[] {name!, string.Join(", ", _oneOf)});
        }
    }
}