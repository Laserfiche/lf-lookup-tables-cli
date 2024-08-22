// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.ODataApi;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;

namespace Laserfiche.LookupTables.Commands
{
    public class CommandReplaceLookupTable : CommandBase
    {
        public CommandReplaceLookupTable() :
            base("replace-lookup-table",
                 "Replaces an existing table with data from a file with supported format such as CSV or XLSX. " +
                 "Supported file formats can be found 'https://api.laserfiche.com/odata4/swagger/index.html?urls.primaryName=v1'. " +
                 "Primary key column \"_key\" cannot be included in the file data.")
        {
            AddOption(CommandLineOptions.TableNameOption);
            AddOption(CommandLineOptions.ProjectScopeOption);
            AddOption(CommandLineOptions.FileOption);
            AddOption(CommandLineOptions.ServicePrincipalKeyOption);
            AddOption(CommandLineOptions.AccessKeyBase64StringOption);

            this.SetHandler(async (
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
                    string filenameWithExtension = Path.GetFileName(file);
                    string scope = ODataUtilities.CreateODataApiScope(false, true, projectScope);
                    ODataApiClient oDataApiClient = ODataUtilities.CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);

                    using Stream tableCsvStream = File.OpenRead(file);
                    var taskId = await oDataApiClient.ReplaceAllRowsAsync(tableName, filenameWithExtension, tableCsvStream);
                    TaskProgress taskProgress = null;
                    await oDataApiClient.MonitorTaskAsync(taskId,
                    (progress) =>
                    {
                        taskProgress = progress;
                        if (IsFailed(progress))
                        {
                            throw new Exception($"Task with id '{taskId}' {progress.Status}." +
                            (progress.Errors != null && progress.Errors.Count > 0 ? " " + System.Text.Json.JsonSerializer.Serialize(progress.Errors) : ""));
                        }

                    });

                    ConsoleWriteLine($"{Name} {taskProgress?.Status}. Task id '{taskId}' completed in {stopwatch.ElapsedMilliseconds}ms. {taskProgress?.Result.ToString()}");

                }
                catch (Exception ex)
                {
                    ConsoleWriteError($"{Name} error. Operation duration {stopwatch.ElapsedMilliseconds}ms. {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            CommandLineOptions.TableNameOption,
            CommandLineOptions.ProjectScopeOption,
            CommandLineOptions.FileOption,
            CommandLineOptions.ServicePrincipalKeyOption,
            CommandLineOptions.AccessKeyBase64StringOption);
        }

        private static bool IsFailed(TaskProgress taskProgress)
        {
            return taskProgress.Status == TaskStatus.Cancelled ||
              taskProgress.Status == TaskStatus.Failed;
        }
    }
}
