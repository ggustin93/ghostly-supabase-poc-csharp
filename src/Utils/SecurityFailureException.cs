using System;

namespace GhostlySupaPoc.Utils
{
    /// <summary>
    /// Custom exception to be thrown when a security validation test fails.
    /// This helps differentiate between an expected error (like an access denied message)
    /// and a true test failure where a security policy was bypassed.
    /// </summary>
    public class SecurityFailureException : Exception
    {
        public SecurityFailureException(string message) : base(message)
        {
        }
    }
} 