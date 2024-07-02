// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.Api.ODataApi;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Laserfiche.Api
{
    static class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine($"Command line help is available via the --help command line argument.\r\n");
            RootCommand rootCommand = ConfigureCommandLineParser();
            return await rootCommand.InvokeAsync(args);
        }

        public static async Task ExportLookupTableCsvAsync(
           ApiClientConfiguration config,
           string tableName,
           string projectScope,
           string file,
           CancellationToken cancel = default)
        {
            tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
            projectScope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));

            if (file == null)
                throw new ArgumentNullException(nameof(file));

            string scope = $"table.Read {projectScope}";
            ODataApiClient oDataApiClient = ODataApiClient.CreateFromServicePrincipalKey(
                config.ServicePrincipalKey,
                config.AccessKey,
                scope);

            Dictionary<string, Entity> entityDictionary = await oDataApiClient.GetTableMetadataAsync();

            if (!entityDictionary.TryGetValue(tableName, out Entity entity))
            {
                throw new Exception($"Lookup table '{tableName}' not found. Verify that the table exists and the Process Automation project scope containing the table is specified.");
            };

            string csv = await ODataApiClientExamples.ExportLookupTableCsvAsync(oDataApiClient, entity);
            await File.WriteAllTextAsync(file, csv, cancel);
        }

        private static ApiClientConfiguration GetApiClientConfiguration()
        {
            ApiClientConfiguration config = new(".env");
            if (config.AuthorizationType != AuthorizationType.CLOUD_ACCESS_KEY)
            {
                throw new Exception("'Laserfiche.Repository.Api.Client.V2' is not supported with self-hosted API Server. Please use 'Laserfiche.Repository.Api.Client' NuGet package");
            }
            return config;
        }

        private static RootCommand ConfigureCommandLineParser()
        {
            var tableNameOption = new Option<string>(
                name: "--tableName",
                description: "Lookup Table name to import export");

            var projectScopeOption = new Option<string>(
                name: "--projectScope",
                description: " Process Automation project scope containing the table specified. E.g. 'project/Global'");

            var fileOption = new Option<string>(
                name: "--file",
                description: "CSV file full path to import or export");

            var rootCommand = new RootCommand("Laserfiche Api use cases and sample code.");

            var exportLookupTableCommand = new Command("ExportLookupTable", "Export a Lookup Table to a CSV file.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption
                };

            var replaceLookupTableCommand = new Command("ReplaceLookupTable", "Replace a Lookup Table using a CSV file.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption
                };

            rootCommand.AddCommand(exportLookupTableCommand);
            rootCommand.AddCommand(replaceLookupTableCommand);

            exportLookupTableCommand.SetHandler(async (tableName, projectScope, file) =>
                {
                    await ExportLookupTableCsvAsync(GetApiClientConfiguration(), tableName, projectScope, file);
                },
                tableNameOption,
                projectScopeOption,
                fileOption);

            replaceLookupTableCommand.SetHandler(async (tableName, projectScope, file) =>
            {
                throw new NotImplementedException();
            },
               tableNameOption,
               projectScopeOption,
               fileOption);

            return rootCommand;
        }
    }
}
