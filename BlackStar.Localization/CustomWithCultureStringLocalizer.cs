/*
Copyright 2017 Oliver Voutat

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BlackStar.Localization
{
    public class CustomWithCultureStringLocalizer : CustomStringLocalizer
    {
        private readonly CultureInfo _culture;

        /// <summary>
        /// Creates a new <see cref="CustomWithCultureStringLocalizer"/>.
        /// </summary>
        /// <param name="dataManager">The <see cref="IDataManager"/> to read strings from.</param>
        /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        /// <param name="culture">The specific <see cref="CultureInfo"/> to use.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public CustomWithCultureStringLocalizer(
            IDataManager dataManager,
            string baseName,
            CultureInfo culture,
            ILogger logger)
            : base(dataManager, baseName, logger)
        {
            if (dataManager == null)
            {
                throw new ArgumentNullException(nameof(dataManager));
            }

            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }

            _culture = culture ?? throw new ArgumentNullException(nameof(culture));
        }

        /// <inheritdoc />
        public override LocalizedString this[string name]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                var value = GetStringSafely(name, _culture);

                return new LocalizedString(name, value ?? name);
            }
        }

        /// <inheritdoc />
        public override LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                var format = GetStringSafely(name, _culture);
                var value = string.Format(_culture, format ?? name, arguments);

                return new LocalizedString(name, value ?? name, resourceNotFound: format == null);
            }
        }

        /// <inheritdoc />
        public override IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            GetAllStrings(includeParentCultures, _culture);
    }
}
