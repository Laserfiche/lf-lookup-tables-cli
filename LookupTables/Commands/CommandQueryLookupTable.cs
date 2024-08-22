// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.ODataApi;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Laserfiche.LookupTables.Commands
{
    public class CommandQueryLookupTable : CommandBase
    {
        public CommandQueryLookupTable() :
            base("query-lookup-table",
                 "Query a Lookup Table and optionally saves the result in a CSV or JSON file.")
        {
            AddOption(CommandLineOptions.TableNameOption);
            AddOption(CommandLineOptions.ProjectScopeOption);
            AddOption(CommandLineOptions.FileOption);
            AddOption(CommandLineOptions.ServicePrincipalKeyOption);
            AddOption(CommandLineOptions.AccessKeyBase64StringOption);
            AddOption(CommandLineOptions.OutputFormatOption);
            AddOption(CommandLineOptions.FilterOption);

            this.SetHandler(async (
                tableName,
                projectScope,
                file,
                servicePrincipalKey,
                accessKeyBase64String,
                outputFormat,
                filter) =>
            {
                var stopwatch = Stopwatch.StartNew();
                int rowCount = 0;
                try
                {
                    tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
                    string scope = ODataUtilities.CreateODataApiScope(true, false, projectScope);
                    ODataApiClient oDataApiClient = ODataUtilities.CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
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
                        case FileFormat.CSV_NO_HEADER:
                        case FileFormat.CSV:
                            if (string.IsNullOrWhiteSpace(select))
                            {
                                IList<string> columnNames = await GetTableColumnNamesExcludingKey(tableName, oDataApiClient);
                                select = string.Join(ODataUtilities.CSV_COMMA_SEPARATOR, columnNames);
                                header = select + Environment.NewLine;
                            }
                            else
                            {
                                header = string.Join(ODataUtilities.CSV_COMMA_SEPARATOR, select.Split(ODataUtilities.CSV_COMMA_SEPARATOR).Select(r => r.Trim())) + Environment.NewLine;
                            }

                            if (outputFormat == FileFormat.CSV_NO_HEADER)
                                header = "";
                            footer = "";
                            rowSeparator = Environment.NewLine;
                            jsonElementRowToStringFunc = (jsonElementRow) => jsonElementRow.ToCsv();
                            break;
                        case FileFormat.JSON:
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
                            }
                        },
                        new ODataQueryParameters { Select = select, Filter = filter });

                    await outputTextWriter.WriteAsync(footer);

                    if (outputToFile)
                    {
                        ConsoleWriteLine($"{Name} exported {rowCount} rows in {stopwatch.ElapsedMilliseconds}ms.");
                    }
                }
                catch (Exception ex)
                {
                    ConsoleWriteError($"{Name} error{(rowCount > 0 ? " at row: " + rowCount.ToString() : "")}. {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            CommandLineOptions.TableNameOption,
            CommandLineOptions.ProjectScopeOption,
            CommandLineOptions.FileOption,
            CommandLineOptions.ServicePrincipalKeyOption,
            CommandLineOptions.AccessKeyBase64StringOption,
            CommandLineOptions.OutputFormatOption,
            CommandLineOptions.FilterOption);
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
    }
}
