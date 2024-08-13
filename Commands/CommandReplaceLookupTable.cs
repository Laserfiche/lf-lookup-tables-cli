// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.ODataApi;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;

namespace Laserfiche.LookupTables.Commands
{
    public class CommandReplaceLookupTable
    {
        public static Command Create(
           Option<string> tableNameOption,
           Option<string> projectScopeOption,
           Option<string> fileOption,
           Option<string> servicePrincipalKeyOption,
           Option<string> accessKeyBase64StringOption)
        {
            const string commandName = "Replace-LookupTable";
            var command = new Command(commandName, "Replaces an existing table with data from a file with supported format. " +
                "Supported file formats can be found 'https://api.laserfiche.com/odata4/swagger/index.html?urls.primaryName=v1'. " +
                "Primary key column \"_key\" cannot be included in the file data.")
                {
                    tableNameOption,
                    projectScopeOption,
                    fileOption,
                    servicePrincipalKeyOption,
                    accessKeyBase64StringOption
                };

            command.SetHandler(async (
                tableName,
                projectScope,
                file,
                servicePrincipalKey,
                accessKeyBase64String) =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    tableName = tableName?.Trim() ?? throw new ArgumentNullException(nameof(tableName));
                    file = file?.Trim() ?? throw new ArgumentNullException(nameof(file));
                    string scope = ODataUtilities.CreateODataApiScope(false, true, projectScope);
                    ODataApiClient oDataApiClient = ODataUtilities.CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);

                    using Stream tableCsvStream = File.Create(file, 4096, FileOptions.SequentialScan);
                    var taskId = await oDataApiClient.ReplaceAllRowsAsync(tableName, tableCsvStream);

                    await oDataApiClient.MonitorTaskAsync(taskId,
                    (taskProgress) =>
                    {
                        Console.WriteLine($" > Task with id '{taskId}' {taskProgress.Status}." +
                            (taskProgress.Result != null ? " " + System.Text.Json.JsonSerializer.Serialize(taskProgress.Result) : "") +
                            (taskProgress.Errors != null && taskProgress.Errors.Count > 0 ? " " + System.Text.Json.JsonSerializer.Serialize(taskProgress.Errors) : ""));
                    });

                }
                catch (Exception ex)
                {
                    Program.ConsoleWriteError($"{commandName} error. {ex.Message}");
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
    }
}
