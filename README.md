# LookupTables Command Line Utility

CLI to interact with Laserfiche Lookup Tables. 

User [Documentation](https://developer.laserfiche.com/docs/guides/guide-lookup-tables-cli/)


## Code Maintenance Prerequisites

- .NET 8.0 core SDK
- Laserfiche Cloud account
- C# IDE such as Visual Studio Code

## Build and Run this App

To compile, and execute this program which will print out the help documentation in the output window:
- Open a terminal window.
- Enter the following commands:

```bat
dotnet build
cd .\LookupTables\
dotnet run
```

### Option to store credentials in an .env file

Credentials can be passed in as command line parameters or can be stored in a file named `.env`.
Place the `.env` file in the `.\LookupTables` subdirectory so that the build process will copy it to the output directory.
See [Passing credential to CLI tool](https://developer.laserfiche.com/docs/guides/guide-lookup-tables-cli/#passing-credentials-to-cli-tool) for details.

**NOTE:** DO NOT check-in the `.env` file in Git. This file contains credentials to gain access to the system.