// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Laserfiche.LookupTables.Commands;
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
            var rootCommand = new RootCommand("Laserfiche Lookup Tables command line utility.");
            rootCommand.AddCommand(new CommandListLookupTables());
            rootCommand.AddCommand(new CommandQueryLookupTable());
            rootCommand.AddCommand(new CommandReplaceLookupTable());

            return rootCommand;
        }
    }
}
