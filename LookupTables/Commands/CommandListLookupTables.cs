// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.ODataApi;
using System;
using System.CommandLine;

namespace Laserfiche.LookupTables.Commands
{
    public class CommandListLookupTables
    {
        public static Command Create(
           Option<string> projectScopeOption,
           Option<string> servicePrincipalKeyOption,
           Option<string> accessKeyBase64StringOption)
        {
            const string commandName = "List-LookupTables";
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
                    string scope = ODataUtilities.CreateODataApiScope(true, false, projectScope);
                    ODataApiClient oDataApiClient = ODataUtilities.CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
                    var tableNames = await oDataApiClient.GetLookupTableNamesAsync();
                    foreach (var tableName in tableNames)
                    {
                        Console.WriteLine(tableName);
                    }
                }
                catch (Exception ex)
                {
                    Program.ConsoleWriteError($"{commandName} error: {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            projectScopeOption,
            servicePrincipalKeyOption,
            accessKeyBase64StringOption);

            return command;
        }
    }
}
