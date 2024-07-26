// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Laserfiche.Api.ODataApi
{
    /// <summary>
    /// Laserfiche OData API usage examples
    /// </summary>
    static class ODataApiClientExamples
    {
        const string ALL_DATA_TYPES_TABLE_SAMPLE_lookup_table_name = "ALL_DATA_TYPES_TABLE_SAMPLE";


        private static async Task<string> ReplaceLookupTableAsync(
           ODataApiClient oDataApiClient,
           Entity allDataTypesEntity,
           string csv)
        {
            Console.WriteLine($"\nReplacing Lookup table {allDataTypesEntity.Name}...");

            var taskId = await oDataApiClient.ReplaceAllRowsAsync(
                allDataTypesEntity.Name,
                new MemoryStream(Encoding.UTF8.GetBytes(csv)));

            return taskId;
        }
        private static async Task MonitorReplaceLookupTableTaskAsync(
            ODataApiClient oDataApiClient,
            string taskId)
        {
            await oDataApiClient.MonitorTaskAsync(taskId,
                (taskProgress) =>
                {
                    Console.WriteLine($" > Task with id '{taskId}' {taskProgress.Status}." +
                        (taskProgress.Result != null ? " " + System.Text.Json.JsonSerializer.Serialize(taskProgress.Result) : "") +
                        (taskProgress.Errors != null && taskProgress.Errors.Count > 0 ? " " + System.Text.Json.JsonSerializer.Serialize(taskProgress.Errors) : ""));
                });
        }
    }
}