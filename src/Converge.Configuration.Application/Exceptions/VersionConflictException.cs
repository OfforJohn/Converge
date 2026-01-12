using System;

namespace Converge.Configuration.Application.Exceptions
{
    public class VersionConflictException : Exception
    {
        public string Key { get; }
        public int ExpectedVersion { get; }
        public int ActualVersion { get; }

        public VersionConflictException(string key, int expectedVersion, int actualVersion)
            : base($"Version conflict for key '{key}': expected {expectedVersion}, actual {actualVersion}.")
        {
            Key = key;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }
    }
}
