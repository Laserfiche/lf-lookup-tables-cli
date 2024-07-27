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
            Option<string> tableNameOption = new(
                name: "--tableName",
                description: "Lookup Table name");

            Option<string> projectScopeOption = new(
                name: "--projectScope",
                description: "Process Automation project scope containing the table specified. E.g. 'project/Global'");

            Option<string> fileOption = new(
                name: "--file",
                description: "CSV file full path to import or export");

            Option<string> servicePrincipalKeyOption = new(
                name: "--servicePrincipalKey",
                description: "Laserfiche Service Principal Key");

            Option<string> accessKeyBase64StringOption = new(
                name: "--accessKeyBase64String",
                description: "Service App AccessKeyBase64String");

            Option<Format> outputFormatOption = new(
               name: "--outputFormat",
               () => Format.JSON,
               description: "Output Format");

            // Command line commands
            var rootCommand = new RootCommand("Laserfiche Lookup Tables command line utility.");

            rootCommand.AddCommand(CreateCommand_GetLookupTables(
                projectScopeOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption));

            rootCommand.AddCommand(CreateCommand_QueryLookupTable(
                tableNameOption,
                projectScopeOption,
                fileOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption,
                outputFormatOption));



            return rootCommand;
        }

        private static Command CreateCommand_GetLookupTables(
            Option<string> projectScopeOption,
            Option<string> servicePrincipalKeyOption,
            Option<string> accessKeyBase64StringOption)
        {
            const string commandName = "GetLookupTables";
            var command = new Command(commandName, "Gets all the lookup tables accessible by the service principal in the provided project scope.")
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
                    ConsoleWriteError($"{commandName} error: {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            projectScopeOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption);

            return command;
        }

        private static Command CreateCommand_QueryLookupTable(
            Option<string> tableNameOption,
            Option<string> projectScopeOption,
            Option<string> fileOption,
            Option<string> servicePrincipalKeyOption,
            Option<string> accessKeyBase64StringOption,
            Option<Format> outputFormatOption)
        {
            const string commandName = "QueryLookupTable";
            var command = new Command(commandName, "Query Lookup Table.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption,
                    servicePrincipalKeyOption,
                    accessKeyBase64StringOption,
                    outputFormatOption
                };

            command.SetHandler(async (tableName, projectScope, file, servicePrincipalKey, accessKeyBase64String, outputFormat) =>
            {
                var stopwatch = new Stopwatch();
                int rowCount = 0;
                try
                {
                    tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
                    string scope = CreateODataApiScope(true, false, projectScope);
                    ODataApiClient oDataApiClient = CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
                    bool outputToFile = !string.IsNullOrWhiteSpace(file);
                    using Stream outputFileStream = outputToFile ? File.Create(file, 4096, FileOptions.WriteThrough) : null;
                    using TextWriter outputTextWriter = outputFileStream != null ? new StreamWriter(outputFileStream, Encoding.UTF8) : Console.Out;
                    string select = null;
                    string header;
                    string footer;
                    string rowSeparator;
                    Func<JsonElement, string> jsonElementRowToStringFunc;

                    switch (outputFormat)
                    {
                        case Format.CSV:
                            if (string.IsNullOrWhiteSpace(select))
                            {
                                IList<string> columnNames = await GetTableColumnNamesExcludingKey(tableName, oDataApiClient);
                                select = string.Join(ODataUtilities.CSV_COMMA_SEPARATOR, columnNames);
                                header = select + Environment.NewLine;
                            }
                            else
                            {
                                header = "";
                            }
                            footer = "";
                            rowSeparator = Environment.NewLine;
                            jsonElementRowToStringFunc = (jsonElementRow) => jsonElementRow.ToCsv();
                            break;
                        case Format.JSON:
                            select = null;
                            header = "[" + Environment.NewLine;
                            footer = Environment.NewLine + "]";
                            rowSeparator = "," + Environment.NewLine;
                            jsonElementRowToStringFunc = (jsonElementRow) => jsonElementRow.ToString();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(outputFormat));
                    }
                    await outputTextWriter.WriteAsync(header);

                    await oDataApiClient.QueryLookupTableAsync(
                        tableName,
                        async (jsonElementRow) =>
                        {
                            string rowTxt = jsonElementRowToStringFunc(jsonElementRow);
                            if (!string.IsNullOrWhiteSpace(rowTxt))
                            {
                                bool isFirstRow = rowCount == 0;
                                rowCount++;
                                if (!isFirstRow)
                                {
                                    await outputTextWriter.WriteAsync(rowSeparator);
                                }
                                await outputTextWriter.WriteAsync(rowTxt);
                                rowCount++;
                            }
                        },
                        new ODataQueryParameters { Select = select });

                    await outputTextWriter.WriteAsync(footer);

                    if (outputToFile)
                    {
                        Console.WriteLine($"{commandName} exported {rowCount} rows in {stopwatch.ElapsedMilliseconds}ms.");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleWriteError($"{commandName} error{(rowCount > 0 ? " at row: " + rowCount.ToString() : "")}. {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            tableNameOption,
            projectScopeOption,
            fileOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption,
            outputFormatOption);
            return command;
        }

        private static async Task<IList<string>> GetTableColumnNamesExcludingKey(string tableName, ODataApiClient oDataApiClient)
        {
            Dictionary<string, Entity> entityDictionary = await oDataApiClient.GetTableMetadataAsync();
            if (!entityDictionary.TryGetValue(tableName, out Entity entity))
            {
                throw new Exception($"Lookup table not found. Verify that the table exists and the Process Automation project scope containing the table is specified.");
            };

            IList<string> columnNames = entity.Properties
                .Select(r => r.Name)
                .Where(r => r != entity.KeyName).ToList();
            return columnNames;
        }

        private static string CreateODataApiScope(bool allowTableRead, bool allowTableWrite, string projectScope)
        {
            string scope = projectScope?.Trim() ?? throw new ArgumentNullException(nameof(projectScope));
            scope = $"{(allowTableRead ? "table.Read " : "")}{(allowTableWrite ? "table.Write " : "")}{scope}";
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

        public enum Format
        {
            JSON = 0,
            CSV = 1
        }
    }
}
