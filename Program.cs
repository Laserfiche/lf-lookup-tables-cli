// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.Api.Client.OAuth;
using Laserfiche.Api.ODataApi;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Laserfiche.Api
{
    static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = ConfigureCommandLineParser();
            return await rootCommand.InvokeAsync(args);
        }

        private static RootCommand ConfigureCommandLineParser()
        {
            // Command line options
            var tableNameOption = new Option<string>(
                name: "--tableName",
                description: "Lookup Table name");

            var projectScopeOption = new Option<string>(
                name: "--projectScope",
                description: "Laserfiche API project scope containing the table specified. E.g. 'project/Global'");

            var fileOption = new Option<string>(
                name: "--file",
                description: "CSV file full path to import or export");

            var servicePrincipalKeyOption = new Option<string>(
                name: "--servicePrincipalKey",
                description: "Laserfiche Service Principal Key");

            var accessKeyBase64StringOption = new Option<string>(
                name: "--accessKeyBase64String",
                description: "Service App AccessKeyBase64String");

            // Command line commands
            var rootCommand = new RootCommand("Laserfiche Api use cases and sample code.");

            rootCommand.AddCommand(CreateCommand_GetLookupTables(
                projectScopeOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption));

            rootCommand.AddCommand(CreateCommand_ExportLookupTableAsCSV(
                tableNameOption,
                projectScopeOption,
                fileOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption));



            return rootCommand;
        }

        private static Command CreateCommand_GetLookupTables(
            Option<string> projectScopeOption,
            Option<string> servicePrincipalKeyOption,
            Option<string> accessKeyBase64StringOption)
        {
            var command = new Command("GetLookupTables", "Gets all the lookup tables accessible by the service principal in the provided project scope.")
                {
                    projectScopeOption,
                    servicePrincipalKeyOption,
                    accessKeyBase64StringOption
                };

            command.SetHandler(async (projectScope, servicePrincipalKey, accessKeyBase64String) =>
            {
                projectScope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));
                string scope = $"table.Read {projectScope}";
                ODataApiClient oDataApiClient = CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
                var tableNames = await oDataApiClient.GetLookupTableNamesAsync();
                foreach (var tableName in tableNames)
                {
                    Console.WriteLine(tableName);
                }
            },
            projectScopeOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption);

            return command;
        }

        private static Command CreateCommand_ExportLookupTableAsCSV(
            Option<string> tableNameOption,
            Option<string> projectScopeOption,
            Option<string> fileOption,
            Option<string> servicePrincipalKeyOption,
            Option<string> accessKeyBase64StringOption)
        {
            var command = new Command("ExportLookupTableAsCSV", "Export a Lookup Table to a CSV file.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption,
                    servicePrincipalKeyOption,
                    accessKeyBase64StringOption
                };

            command.SetHandler(async (tableName, projectScope, file, servicePrincipalKey, accessKeyBase64String) =>
            {
                tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
                projectScope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));
                file = file?.Trim() ?? throw new ArgumentNullException(nameof(file));

                string scope = $"table.Read {projectScope}";
                ODataApiClient oDataApiClient = CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);

                Dictionary<string, Entity> entityDictionary = await oDataApiClient.GetTableMetadataAsync();

                if (!entityDictionary.TryGetValue(tableName, out Entity entity))
                {
                    throw new Exception($"Lookup table '{tableName}' not found. Verify that the table exists and the Process Automation project scope containing the table is specified.");
                };

                string csv = await ODataApiClientExamples.ExportLookupTableCsvAsync(oDataApiClient, entity);
                await File.WriteAllTextAsync(file, csv);
            },
            tableNameOption,
            projectScopeOption,
            fileOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption);

            return command;
        }

        private static ODataApiClient CreateODataApiClient(string servicePrincipalKey, string accessKeyBase64String, string scope)
        {
            {
                if (string.IsNullOrEmpty(scope)) throw new ArgumentNullException(nameof(scope));
                ApiClientConfiguration config = new(".env");
                AccessKey accessKey;
                if (servicePrincipalKey != null || accessKeyBase64String != null)
                {
                    accessKey = AccessKey.CreateFromBase64EncodedAccessKey(accessKeyBase64String);
                }
                else if (config.AccessKey != null && config.ServicePrincipalKey != null)
                {
                    servicePrincipalKey = config.ServicePrincipalKey;
                    accessKey = config.AccessKey;
                }
                else
                {
                    accessKey = null;
                }

                if (string.IsNullOrEmpty(servicePrincipalKey)) throw new ArgumentNullException(nameof(servicePrincipalKey));
                if (accessKey == null) throw new ArgumentNullException(nameof(accessKeyBase64String));

                ODataApiClient oDataApiClient = ODataApiClient.CreateFromServicePrincipalKey(
                    servicePrincipalKey,
                    config.AccessKey,
                    scope);

                return oDataApiClient;
            }

        }
    }
}
