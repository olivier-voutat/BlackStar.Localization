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
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace BlackStar.Localization
{
    public class SqlDataManager : IDataManager
    {
        private readonly string _baseName;
        private readonly Dictionary<string, string> _dbArgs;
        private readonly ILogger _logger;

        private readonly List<CustomStringData> _stringData;

        /// <summary>
        /// Creates a new <see cref="SqlDataManager"/>.
        /// </summary>
        /// <param name="baseName">The base name used to search the strings.</param>
        /// <param name="dbArgs">Database information where to get the data.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        internal SqlDataManager(string baseName, Dictionary<string, string> dbArgs, ILogger logger)
        {
            if (!dbArgs.ContainsKey("ConnectionString"))
            {
                throw new KeyNotFoundException("ConnectionString information missing in source arguments.");
            }

            if (!dbArgs.ContainsKey("Table"))
            {
                throw new KeyNotFoundException("Table information missing in source arguments.");
            }

            if (!dbArgs.ContainsKey("Column"))
            {
                throw new KeyNotFoundException("Column information missing in source arguments.");
            }

            _baseName = baseName;
            _dbArgs = dbArgs;
            _logger = logger;

            _stringData = new List<CustomStringData>();
        }

        /// <summary>
        /// Returns the value of the specified string from the database.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        public string GetString(string name)
        {
            return GetString(name, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns the value of the specified string from the database localized for the specified culture.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get the string for.</param>
        /// <returns>The resource string, or <c>null</c> if none was found.</returns>
        public string GetString(string name, CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));

            InitializeData();

            CustomStringData result = _stringData.FirstOrDefault(x => x.CultureName == culture.Name && x.Name == name);
            if (result != null) return result.Value;

            return _stringData.FirstOrDefault(x => string.IsNullOrEmpty(x.CultureName) && x.Name == name)?.Value;
        }

        /// <summary>
        /// Returns all names in the current culture.
        /// </summary>
        /// <param name="includeParentCultures">Include parent cultures.</param>
        /// <returns>The strings.</returns>
        public IEnumerable<string> GetAllNames(bool includeParentCultures)
        {
            return GetAllNames(includeParentCultures, CultureInfo.CurrentUICulture);
        }

        /// <summary>
        /// Returns all names in the specified culture.
        /// </summary>
        /// <param name="includeParentCultures">Include parent cultures.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to get strings for.</param>
        /// <returns>The strings.</returns>
        public IEnumerable<string> GetAllNames(bool includeParentCultures, CultureInfo culture)
        {
            if (culture == null) throw new ArgumentNullException(nameof(culture));

            InitializeData();

            if (includeParentCultures)
            {
                foreach (var s in _stringData.Where(x => new CultureInfo(x.CultureName).Parent == new CultureInfo(culture.Name).Parent))
                {
                    yield return s.Name;
                }
            }
            else
            {
                foreach (var s in _stringData.Where(x => x.CultureName == culture.Name))
                {
                    yield return s.Name;
                }
            }
        }

        private void InitializeData()
        {
            if (_stringData.Any()) return;

            using (SqlConnection con = new SqlConnection(_dbArgs["ConnectionString"]))
            {
                using (SqlCommand cmd = new SqlCommand($"SELECT CultureName, ResourceKey, {_dbArgs["Column"]} FROM {_dbArgs["Table"]} WHERE Path LIKE @path", con))
                {
                    try
                    {
                        var indexes = Regex.Matches(_baseName, "\\.").Cast<Match>().Select(m => m.Index).ToArray();
                        cmd.Parameters.AddWithValue("@path", _baseName.Substring(indexes[indexes.Length - 2] + 1));
                        cmd.Connection.Open();
                        var r = cmd.ExecuteReader();
                        while (r.Read())
                        {
                            _stringData.Add(new CustomStringData()
                            {
                                CultureName = r["CultureName"].ToString(),
                                Name = r["ResourceKey"].ToString(),
                                Value = r[_dbArgs["Column"]].ToString()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical("Connection string: {0}; Datatable: {1}; Column: {2}; Exception Message: {3}; Exception StackTrace: {4}",
                            _dbArgs["ConnectionString"],
                            _dbArgs["Table"],
                            _dbArgs["Column"],
                            ex.Message,
                            ex.StackTrace);

                        throw;
                    }
                }
            }
        }

        private class CustomStringData
        {
            public string CultureName { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }
        }
    }
}
