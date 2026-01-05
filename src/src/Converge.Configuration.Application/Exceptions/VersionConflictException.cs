using System;

namespace Converge.Configuration.Application.Exceptions
{
    public class VersionConflictException : Exception
    {
        public string Key { get; }
        public int Expected { get; }
        public int Actual { get; }

        public VersionConflictException(string key, int expected, int actual)
            : base($"Version conflict for '{key}'. Expected {expected}, actual {actual}.")
        {
            Key = key;
            Expected = expected;
            Actual = actual;
        }
    }
}
