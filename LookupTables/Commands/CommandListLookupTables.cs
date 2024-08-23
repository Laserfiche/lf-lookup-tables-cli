// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.ODataApi;
using System;
using System.CommandLine;
using System.Xml.Linq;

namespace Laserfiche.LookupTables.Commands
{
    public class CommandListLookupTables : CommandBase
    {
        public CommandListLookupTables() :
            base("list-lookup-tables",
                 "Lists all the lookup tables names accessible by the service principal in the provided project scope.")
        {
            AddOption(CommandLineOptions.ProjectScopeOption);
            AddOption(CommandLineOptions.ServicePrincipalKeyOption);
            AddOption(CommandLineOptions.AccessKeyBase64StringOption);

            this.SetHandler(async (projectScope, servicePrincipalKey, accessKeyBase64String) =>
            {
                try
                {
                    string scope = ODataUtilities.CreateODataApiScope(true, false, projectScope);
                    ODataApiClient oDataApiClient = ODataUtilities.CreateODataApiClient(servicePrincipalKey, accessKeyBase64String, scope);
                    var tableNames = await oDataApiClient.GetLookupTableNamesAsync();
                    foreach (var tableName in tableNames)
                    {
                        ConsoleWriteLine(tableName);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleWriteError($"{Name} error: {ex.Message}");
                    System.Environment.Exit(1);
                }
            },
            CommandLineOptions.ProjectScopeOption,
            CommandLineOptions.ServicePrincipalKeyOption,
            CommandLineOptions.AccessKeyBase64StringOption);
        }
    }
}
