// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.Commands;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Laserfiche.LookupTables
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
                name: "--table-name",
                description: "Lookup Table name");

            Option<string> projectScopeOption = new(
                name: "--project-scope",
                description: "Process Automation project scope containing the table specified. E.g. 'project/Global'");

            Option<string> fileOption = new(
                name: "--file",
                description: "File full path to import or export");

            Option<string> servicePrincipalKeyOption = new(
                name: "--service-principal-key",
                description: "Laserfiche Service Principal Key");

            Option<string> accessKeyBase64StringOption = new(
                name: "--access-key-base64string",
                description: "Service App AccessKeyBase64String");

            Option<DataFormat> outputFormatOption = new(
               name: "--output-format",
               () => DataFormat.JSON,
               description: "Output Format");

            Option<string> filterOption = new(
               name: "--filter",
               description: "Query filter conforming to OData v4 syntax. See: https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_BuiltinFilterOperations"
                + " - 'gt' Greater than: 'Price gt 20'"
                + " - 'in' Is a member of: 'City in ('Roma', 'London')'");

            Option<bool> includeColumnsHeaderOption = new(
                name: "--include-columns-header",
                () => true,
                description: "Includes the column header is a row in the output, if applicable.");

            // Command line commands
            var rootCommand = new RootCommand("Laserfiche Lookup Tables command line utility.");

            rootCommand.AddCommand(CommandListLookupTables.Create(
                projectScopeOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption));

            rootCommand.AddCommand(CommandQueryLookupTable.Create(
                tableNameOption,
                projectScopeOption,
                fileOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption,
                outputFormatOption,
                filterOption,
                includeColumnsHeaderOption));

            rootCommand.AddCommand(CommandReplaceLookupTable.Create(
                tableNameOption,
                projectScopeOption,
                fileOption,
                servicePrincipalKeyOption,
                accessKeyBase64StringOption));

            return rootCommand;
        }

        public static void ConsoleWriteError(string msg)
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
