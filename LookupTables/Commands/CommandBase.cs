// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.CommandLine;

namespace Laserfiche.LookupTables.Commands
{
    public abstract class CommandBase : Command
    {
        protected CommandBase(string name, string description) :
            base(name, description)
        {
        }

        protected void ConsoleWriteLine(string message)
        {
            Console.WriteLine(message);
        }

        protected void ConsoleWriteError(string message)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}

