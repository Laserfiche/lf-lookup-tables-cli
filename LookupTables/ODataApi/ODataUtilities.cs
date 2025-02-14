﻿// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.Api.Client.OAuth;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Laserfiche.LookupTables.ODataApi

{
    public static class ODataUtilities
    {
        public const char CSV_COMMA_SEPARATOR = ',';

        public static string CreateODataApiScope(bool allowTableRead, bool allowTableWrite, string projectScope)
        {
            string scope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));
            scope = $"{(allowTableRead ? "table.Read " : "")}{(allowTableWrite ? "table.Write " : "")}{scope}";
            return scope.Trim();
        }

        public static string GetStringPropertyValue(this JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement nameElement))
            {
                var value = nameElement.GetString();
                return value;
            }
            return null;
        }

        static public Dictionary<string, Entity> EdmXmlToEntityDictionary(XDocument edmXml)
        {
            XElement schema = edmXml.Descendants().Where(x => x.Name.LocalName == "Schema").FirstOrDefault();
            int v = 0;
            string z = v.GetType().ToString();
            XNamespace ns = schema.GetDefaultNamespace();
            Dictionary<string, Entity> entityTypes = schema.Descendants(ns + "EntityType")
                .Select(x => new Entity()
                {
                    Name = x.Attribute("Name")?.Value,
                    KeyName = x.Descendants(ns + "PropertyRef").FirstOrDefault()?.Attribute("Name")?.Value,
                    Properties = x.Elements(ns + "Property").Select(y => new Property()
                    {
                        Name = y.Attribute("Name")?.Value,
                        SystemType = Type.GetType("System." + ((string)y.Attribute("Type")).Split(new char[] { '.' }).Last()),
                        Nullable = (y.Attribute("Nullable") == null) ? (Boolean?)null : (string)y.Attribute("Nullable") != "false"
                    }).ToList()
                })
                .ToDictionary(x => x.Name, x => x);

            return entityTypes;
        }

        static public string ToCsv(this JsonElement element)
        {
            StringBuilder sb = new();
            if (element.ValueKind != JsonValueKind.Object)
                throw new ArgumentException(nameof(element.ValueKind));

            bool first = true;
            foreach (var property in element.EnumerateObject())
            {
                if (!first)
                {
                    sb.Append(CSV_COMMA_SEPARATOR);
                }
                first = false;
                string strValue = property.Value.ToString();
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        strValue = property.Value.GetString();
                        if (strValue != null && strValue.Any(r => r == CSV_COMMA_SEPARATOR || r == '"'))
                            strValue = "\"" + strValue.Replace("\"", "\"\"") + "\"";
                        break;
                    case JsonValueKind.Number:
                        strValue = property.Value.GetDecimal().ToString(CultureInfo.InvariantCulture);
                        break;
                    case JsonValueKind.False:
                        strValue = "false";
                        break;
                    case JsonValueKind.True:
                        strValue = "true";
                        break;
                    default:
                        strValue = "";
                        break;
                }
                sb.Append(strValue);
            }
            return sb.ToString();
        }


        public static ODataApiClient CreateODataApiClient(string servicePrincipalKey, string accessKeyBase64String, string scope)
        {
            if (string.IsNullOrEmpty(scope)) throw new ArgumentNullException(nameof(scope));

            AccessKey accessKey;
            try
            {
                accessKey = accessKeyBase64String != null ? AccessKey.CreateFromBase64EncodedAccessKey(accessKeyBase64String) : null;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, CommandLineOptions.AccessKeyBase64StringOption.Name);
            }

            if (accessKey == null || servicePrincipalKey == null)
            {
                ApiClientConfiguration config = new(".env");
                servicePrincipalKey ??= config.ServicePrincipalKey;
                accessKey ??= config.AccessKey;
            }

            if (string.IsNullOrEmpty(servicePrincipalKey)) throw new ArgumentNullException(nameof(servicePrincipalKey));
            if (accessKey == null) throw new ArgumentNullException(nameof(accessKeyBase64String));

            ODataApiClient oDataApiClient = ODataApiClient.CreateFromServicePrincipalKey(
                servicePrincipalKey,
                accessKey,
                scope);

            return oDataApiClient;
        }
    }
}
