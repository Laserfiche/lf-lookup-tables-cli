﻿// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.Api.Client;
using Laserfiche.Api.Client.HttpHandlers;
using Laserfiche.Api.Client.OAuth;
using Laserfiche.Api.Client.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Laserfiche.LookupTables.ODataApi
{
    /// <summary>
    /// Laserfiche OData API Client. See https://api.laserfiche.com/odata4/swagger/index.html?urls.primaryName=v1
    /// </summary>
    public class ODataApiClient
    {
        private readonly HttpClient _httpClient;
        private ODataApiClient(IHttpRequestHandler httpRequestHandler)
        {
            ArgumentNullException.ThrowIfNull(httpRequestHandler, nameof(httpRequestHandler));

            var apiHttpMessageHandler = new ApiHttpMessageHandler(
                httpRequestHandler,
                (domain) => DomainUtils.GetODataApiBaseUri(domain));

            _httpClient = new HttpClient(apiHttpMessageHandler)
            {
                BaseAddress = new Uri("http://example.com") //Needed to use relative URLs in http requests.
            };
        }

        /// <summary>
        /// Creates a ODataApiClient given a Service Principal Key and application Access Key
        /// </summary>
        /// <param name="servicePrincipalKey"></param>
        /// <param name="accessKey"></param>
        /// <param name="scope">For example: "table.Read table.Write project/Global"</param>
        /// <returns></returns>
        public static ODataApiClient CreateFromServicePrincipalKey(string servicePrincipalKey, AccessKey accessKey, string scope)
        {
            var httpRequestHandler = new OAuthClientCredentialsHandler(servicePrincipalKey, accessKey, scope);
            return new ODataApiClient(httpRequestHandler);
        }

        /// <summary>
        /// Returns Lookup Table names that are accessible by the current user.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<IList<string>> GetLookupTableNamesAsync(CancellationToken cancel = default)
        {
            var httpResponse = await _httpClient.GetAsync($"/table", cancel);
            httpResponse.EnsureSuccessStatusCode();
            JsonDocument content = await httpResponse.Content.ReadFromJsonAsync<JsonDocument>(JsonSerializerOptions.Default, cancel);
            var value = content.RootElement.GetProperty("value");

            var tableNames = new List<string>();
            foreach (var element in value.EnumerateArray())
            {
                string kind = element.GetStringPropertyValue("kind");
                if (kind == "EntitySet")
                {
                    string name = element.GetStringPropertyValue("name");
                    tableNames.Add(name);
                }
            }
            return tableNames;
        }

        /// <summary>
        /// Returns The Lookup Tables definitions that are accessible by the current user.
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, Entity>> GetTableMetadataAsync(CancellationToken cancel = default)
        {
            var httpResponse = await _httpClient.GetAsync($"/table/$metadata", cancel);
            httpResponse.EnsureSuccessStatusCode();
            using var contentStream = await httpResponse.Content.ReadAsStreamAsync(cancel);
            var edmXml = XDocument.Load(contentStream);
            Dictionary<string, Entity> entityDictionary = ODataUtilities.EdmXmlToEntityDictionary(edmXml);
            return entityDictionary;
        }

        /// <summary>
        /// Query a Lookup Table.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="processTableRow"></param>
        /// <param name="queryParameters"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task QueryLookupTableAsync(
           string tableName,
           Func<JsonElement, Task> processTableRow,
           ODataQueryParameters queryParameters,
           CancellationToken cancel = default)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException(nameof(tableName));

            string url = $"table/{Uri.EscapeDataString(tableName)}";
            if (queryParameters != null)
                url = queryParameters.AppendQueryString(url);

            while (!string.IsNullOrWhiteSpace(url))
            {
                using var httpResponse = await _httpClient.GetAsync(url, cancel);
                httpResponse.EnsureSuccessStatusCode();
                JsonDocument content = await httpResponse.Content.ReadFromJsonAsync<JsonDocument>(JsonSerializerOptions.Default, cancel);
                {
                    foreach (var item in content.RootElement.GetProperty("value").EnumerateArray())
                    {
                        await processTableRow(item);
                    }
                };

                url = content.RootElement.GetStringPropertyValue("@odata.nextLink");
            }
        }

        /// <summary>
        /// Replaces an existing table with data from a file with supported format.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="filenameWithExtension"></param>
        /// <param name="tableContentStream"></param>
        /// <param name="cancel"></param>
        /// <returns>TaskId</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<string> ReplaceAllRowsAsync(
          string tableName,
          string filenameWithExtension,
          Stream tableContentStream,
          CancellationToken cancel = default)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(tableName, nameof(tableName));
            ArgumentNullException.ThrowIfNullOrWhiteSpace(filenameWithExtension, nameof(filenameWithExtension));
            ArgumentNullException.ThrowIfNull(tableContentStream, nameof(tableContentStream));
            string url = $"table/{Uri.EscapeDataString(tableName)}/ReplaceAllRowsAsync";

            using var multipartContent = new MultipartFormDataContent("-N891KdKd7Yk");
            multipartContent.Headers.ContentType.MediaType = "multipart/form-data";
            using var streamContent = new StreamContent(tableContentStream);
            multipartContent.Add(streamContent, "file", filenameWithExtension);
            var httpResponse = await _httpClient.PostAsync(url, multipartContent, cancel);
            httpResponse.EnsureSuccessStatusCode();
            JsonDocument content = await httpResponse.Content.ReadFromJsonAsync<JsonDocument>(JsonSerializerOptions.Default, cancel);
            var taskId = content.RootElement.GetStringPropertyValue("taskId");
            return taskId;
        }


        /// <summary>
        /// Monitor the progress of a long running task. 
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task MonitorTaskAsync(
            string taskId,
            Action<TaskProgress> handleTaskProgress,
            CancellationToken cancel = default)
        {
            if (string.IsNullOrWhiteSpace(taskId))
                throw new ArgumentNullException(nameof(taskId));

            while (true)
            {
                string url = $"general/Tasks({Uri.EscapeDataString(taskId)})";
                var httpResponse = await _httpClient.GetAsync(url, cancel);
                httpResponse.EnsureSuccessStatusCode();
                var body = await httpResponse.Content.ReadAsStringAsync();
                var taskProgress = JsonSerializer.Deserialize<TaskProgress>(body);
                handleTaskProgress(taskProgress);

                bool done = taskProgress.Status == TaskStatus.Completed || taskProgress.Status == TaskStatus.Failed || taskProgress.Status == TaskStatus.Cancelled;
                if (done)
                    break;

                await Task.Delay(100, cancel);
            }
        }
    }

    public class TaskProgress
    {
        /// <summary>
        /// Task Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The type of the task associated with this TaskProgress.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Determines what percentage of the execution of the associated task is completed.
        /// </summary>
        public int PercentComplete { get; set; }

        /// <summary>
        /// The status of the task associated with this TaskProgress.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TaskStatus Status { get; set; }

        /// <summary>
        /// The list of errors occurred during the execution of the associated task.
        /// </summary>
        public IList<ProblemDetails> Errors { get; set; }

        /// <summary>
        /// The result of the execution of the associated task.
        /// </summary>
        public JsonElement Result { get; set; }

        /// <summary>
        /// The time representing when the associated task's execution started.
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// The time representing when the associated task's status last changed.
        /// </summary>
        public DateTimeOffset LastUpdateTime { get; set; }
    }

    public enum TaskStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Cancelled = 4,
    }

    public class Entity
    {
        public string Name { get; set; }
        public string KeyName { get; set; }
        public List<Property> Properties { get; set; }
    }
    public class Property
    {
        public string Name { get; set; }
        public Type SystemType { get; set; }
        public Boolean? Nullable { get; set; }
    }

    public class ODataQueryParameters
    {
        /// <summary>
        /// Aggregation behavior is triggered using the query option $apply. It takes a sequence of set transformations, separated by forward slashes to express that they are consecutively applied, i.e., the result of each transformation is the input to the next transformation.
        /// </summary>
        public string Apply { get; set; }

        /// <summary>
        /// A function that must evaluate to true for a record to be returned. e.g.: '"first_name eq 'Paolo'"
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Limits the properties returned in the result.
        /// </summary>
        public string Select { get; set; }

        /// <summary>
        /// Specifies the order in which items are returned.
        /// </summary>
        public string Orderby { get; set; }

        /// <summary>
        /// Escaped URL querystring parameters.
        /// </summary>
        /// <returns></returns>
        public string ToQueryString()
        {
            var qsList = new List<string>();
            if (!string.IsNullOrWhiteSpace(Apply))
                qsList.Add($"$apply={Uri.EscapeDataString(Apply)}");

            if (!string.IsNullOrWhiteSpace(Filter))
                qsList.Add($"$filter={Uri.EscapeDataString(Filter)}");

            if (!string.IsNullOrWhiteSpace(Select))
                qsList.Add($"$select={Uri.EscapeDataString(Select)}");

            if (!string.IsNullOrWhiteSpace(Orderby))
                qsList.Add($"$orderby={Uri.EscapeDataString(Orderby)}");

            return qsList.Count == 0 ? null : string.Join('&', qsList);
        }

        /// <summary>
        /// Appends QueryString if any parameter is defined.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string AppendQueryString(string url)
        {
            var qs = ToQueryString();
            if (qs != null)
                url += "?" + qs;

            return url;
        }
    }
}
