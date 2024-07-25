// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.Api.Client.OAuth;
using Laserfiche.Api.ODataApi;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

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
                description: "Process Automation project scope containing the table specified. E.g. 'project/Global'");

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
            var rootCommand = new RootCommand("Laserfiche Lookup Tables command line utility.");

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
                try
                {
                    string scope = CreateODataApiScope(true, false, projectScope);
                    ODataApiClient oDataApiClient = CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
                    var tableNames = await oDataApiClient.GetLookupTableNamesAsync();
                    foreach (var tableName in tableNames)
                    {
                        Console.WriteLine(tableName);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleWriteError(ex.Message);
                    System.Environment.Exit(1);
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
            const string commandName = "ExportLookupTableAsCSV";
            var command = new Command(commandName, "Export a Lookup Table to a CSV file.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption,
                    servicePrincipalKeyOption,
                    accessKeyBase64StringOption
                };

            command.SetHandler(async (tableName, projectScope, file, servicePrincipalKey, accessKeyBase64String) =>
            {
                var stopwatch = new Stopwatch();
                try
                {
                    tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
                    file = file?.Trim() ?? throw new ArgumentNullException(nameof(file));
                    string scope = CreateODataApiScope(true, false, projectScope);

                    ODataApiClient oDataApiClient = CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);

                    Dictionary<string, Entity> entityDictionary = await oDataApiClient.GetTableMetadataAsync();

                    if (!entityDictionary.TryGetValue(tableName, out Entity entity))
                    {
                        throw new Exception($"Lookup table not found. Verify that the table exists and the Process Automation project scope containing the table is specified.");
                    };

                    IList<string> columnNames = entity.Properties
                        .Select(r => r.Name)
                        .Where(r => r != entity.KeyName).ToList();

                    int rowCount = 0;
                    var tableCsv = new StringBuilder();
                    string columnsHeaders = string.Join(ODataUtilities.CSV_COMMA_SEPARATOR, columnNames);
                    tableCsv.AppendLine(columnsHeaders);
                    Action<JsonElement> processTableRow = (tableRow) =>
                    {
                        rowCount++;
                        var rowCsv = tableRow.ToCsv();
                        if (!string.IsNullOrWhiteSpace(rowCsv))
                            tableCsv.AppendLine(rowCsv);
                    };
                    await oDataApiClient.QueryLookupTableAsync(entity.Name, processTableRow,
                        new ODataQueryParameters { Select = columnsHeaders });
                    var csv = tableCsv.ToString();
                    await File.WriteAllTextAsync(file, csv);
                    Console.WriteLine($"{commandName} exported {rowCount} rows.");
                }
                catch (Exception ex)
                {
                    ConsoleWriteError($"{commandName} error {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            tableNameOption,
            projectScopeOption,
            fileOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption);
            return command;
        }

        private static string CreateODataApiScope(bool allowTableRead, bool allowTableWrite, string projectScope)
        {
            string scope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));
            scope = $"{(allowTableRead ? "table.Read " : "")}{(allowTableWrite ? "table.Write " : "")}{projectScope}";
            return scope;
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

        private static void ConsoleWriteError(string msg)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(msg);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
