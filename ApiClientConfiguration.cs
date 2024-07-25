// Copyright Laserfiche.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Laserfiche.Api.Client.OAuth;
using System;
using System.IO;

namespace Laserfiche.Api
{
    internal class ApiClientConfiguration
    {
        internal const string ACCESS_KEY = "ACCESS_KEY";
        internal const string SERVICE_PRINCIPAL_KEY = "SERVICE_PRINCIPAL_KEY";

        public string ServicePrincipalKey { get; set; }
        public AccessKey AccessKey { get; set; }

        public ApiClientConfiguration(string filename)
        {            
            bool dotEnvFileFound = TryLoadFromDotEnv(filename);

            // Read credentials from environment variables.
            ServicePrincipalKey = Environment.GetEnvironmentVariable(SERVICE_PRINCIPAL_KEY);

            var base64EncodedAccessKey = Environment.GetEnvironmentVariable(ACCESS_KEY);
            if (base64EncodedAccessKey != null)
            {
                AccessKey = AccessKey.CreateFromBase64EncodedAccessKey(base64EncodedAccessKey);
            }
        }

        private static bool TryLoadFromDotEnv(string fileName)
        {
            var binFolder = AppDomain.CurrentDomain.BaseDirectory;
            var projectDir = Directory.GetParent(binFolder)?.Parent?.Parent?.Parent?.FullName;
            var path = Path.Combine(projectDir, fileName);
            if (path == null)
            {
                return false;
            }
            else
            {
                if (!File.Exists(path))
                {
                    return false;
                }
            }
            DotNetEnv.Env.Load(path, new DotNetEnv.LoadOptions(
                setEnvVars: true,
                clobberExistingVars: true,
                onlyExactPath: true
            ));
            return true;
        }
    }
}
