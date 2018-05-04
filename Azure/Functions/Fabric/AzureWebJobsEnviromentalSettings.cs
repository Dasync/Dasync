using System;
using System.Collections;
using System.Collections.Generic;

namespace Dasync.Fabric.AzureFunctions
{
    public interface IAzureWebJobsEnviromentalSettings
    {
        string DefaultStorageConnectionString { get; }

        string WebSiteName { get; }

        bool TryGetSetting(string key, out string value);

        IEnumerable<KeyValuePair<string, string>> GetAllSettings();
    }

    public class AzureWebJobsEnviromentalSettings : IAzureWebJobsEnviromentalSettings
    {
        public string DefaultStorageConnectionString =>
            Environment.GetEnvironmentVariable(
                "AzureWebJobsStorage",
                EnvironmentVariableTarget.Process);

        public string WebSiteName =>
            Environment.GetEnvironmentVariable(
                "WEBSITE_SITE_NAME",
                EnvironmentVariableTarget.Process);

        public bool TryGetSetting(string key, out string value)
        {
            value = Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
            return !string.IsNullOrEmpty(value);
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllSettings()
        {
            var dictionary = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            foreach (DictionaryEntry pair in dictionary)
            {
                var key = (string)pair.Key;
                var value = (string)pair.Value;
                yield return new KeyValuePair<string, string>(key, value);
            }
        }
    }
}
