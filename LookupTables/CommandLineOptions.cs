// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.CommandLine;

namespace Laserfiche.LookupTables
{
    public static class CommandLineOptions
    {
        public static readonly Option<string> TableNameOption = new(
                name: "--table-name",
                description: "Lookup Table name");

        public static readonly Option<string> ProjectScopeOption = new(
            name: "--project-scope",
            description: "Process Automation project scope containing the table specified. E.g. 'project/Global'");

        public static readonly Option<string> FileOption = new(
            name: "--file",
            description: "File full path to import or export");

        public static readonly Option<string> ServicePrincipalKeyOption = new(
            name: "--service-principal-key",
            description: "Laserfiche Service Principal Key");

        public static readonly Option<string> AccessKeyBase64StringOption = new(
            name: "--access-key-base64string",
            description: "Service App AccessKeyBase64String");

        public static readonly Option<FileFormat> OutputFormatOption = new(
           name: "--output-format",
           () => FileFormat.JSON,
           description: "Output Format: Json, CSV with column header, CSV without column header.");

        public static readonly Option<string> FilterOption = new(
           name: "--filter",
           description: "Query filter conforming to OData v4 syntax. See: https://docs.oasis-open.org/odata/odata/v4.01/odata-v4.01-part1-protocol.html#sec_BuiltinFilterOperations"
            + " - 'gt' Greater than: 'Price gt 20'"
            + " - 'in' Is a member of: 'City in ('Roma', 'London')'");
    }

    public enum FileFormat
    {
        JSON = 0,
        CSV = 1,
        CSV_NO_HEADER = 2
    }
}
