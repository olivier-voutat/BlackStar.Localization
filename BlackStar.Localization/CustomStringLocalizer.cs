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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BlackStar.Localization
{
    /// <summary>
    /// An <see cref="IStringLocalizer"/> that uses the <see cref="IDataManager"/> to provide localized strings.
    /// </summary>
    /// <remarks>This type is thread-safe.</remarks>
    public class CustomStringLocalizer : IStringLocalizer
    {
        private readonly ConcurrentDictionary<string, object> _missingCache = new ConcurrentDictionary<string, object>();
        private readonly IDataManager _dataManager;
        private readonly string _baseName;
        private readonly ILogger _logger;

        /// <summary>
        /// Creates a new <see cref="CustomStringLocalizer"/>.
        /// </summary>
        /// <param name="dataManager">The <see cref="IDataManager"/> to read strings from.</param>
        /// <param name="baseName">The base name of the embedded resource that contains the strings.</param>
        /// <param name="resourceNamesCache">Cache of the list of strings for a given resource assembly name.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public CustomStringLocalizer(IDataManager dataManager, string baseName, ILogger logger)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _baseName = baseName ?? throw new ArgumentNullException(nameof(baseName));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                var value = GetStringSafely(name, null);

                return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _baseName);
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null) throw new ArgumentNullException(nameof(name));

                var format = GetStringSafely(name, null);
                var value = string.Format(format ?? name, arguments);

                return new LocalizedString(name, value, resourceNotFound: format == null, searchedLocation: _baseName);
            }
        }

        /// <summary>
        /// Creates a new <see cref="CustomStringLocalizer"/> for a specific <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="culture">The <see cref="CultureInfo"/> to use.</param>
        /// <returns>A culture-specific <see cref="CustomStringLocalizer"/>.</returns>
        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            return culture == null
                ? new CustomStringLocalizer(
                    _dataManager,
                    _baseName,
                    _logger)
                : new CustomWithCultureStringLocalizer(
                    _dataManager,
                    _baseName,
                    culture,
                    _logger);
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Returns all strings in the specified culture.
        /// </summary>
        /// <param name="includeParentCultures"></param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The strings.</returns>
        protected IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));

            foreach (var name in _dataManager.GetAllNames(includeParentCultures, culture))
            {
                var value = GetStringSafely(name, culture);
                yield return new LocalizedString(name, value ?? name, resourceNotFound: value == null, searchedLocation: _baseName);
            }
        }

        /// <summary>
        /// Gets a resource string from the <see cref="_dataManager"/> and returns <c>null</c> instead of
        /// throwing exceptions if a match isn't found.
        /// </summary>
        /// <param name="name">The name of the string resource.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        protected string GetStringSafely(string name, CultureInfo culture)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var keyCulture = culture ?? CultureInfo.CurrentUICulture;

            var cacheKey = $"name={name}&culture={keyCulture.Name}";

            _logger.LogDebug($"{nameof(ResourceManagerStringLocalizer)} searched for '{name}' in '{_baseName}' with culture '{keyCulture}'.");

            if (_missingCache.ContainsKey(cacheKey)) return null;

            try
            {
                return culture == null ? _dataManager.GetString(name) : _dataManager.GetString(name, culture);
            }
            catch (Exception)
            {
                _missingCache.TryAdd(cacheKey, null);
                return null;
            }
        }
    }
}
