using System;

namespace Converge.Configuration.Application.Exceptions
{
    public class ConfigurationAlreadyExistsException : Exception
    {
        public string Key { get; }

        public ConfigurationAlreadyExistsException(string key)
            : base($"Configuration '{key}' already exists. Use update instead.")
        {
            Key = key;
        }
    }
}
