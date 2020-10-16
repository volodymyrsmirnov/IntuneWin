using System;

namespace IntuneWin.Exceptions
{
    /// <summary>
    /// Invalid IntuneWin file format.
    /// </summary>
    public class IntuneWinInvalidFileException : Exception
    {
        /// <summary>
        /// Create invalid IntuneWin file format exception.
        /// </summary>
        /// <param name="exception">Inner exception.</param>
        public IntuneWinInvalidFileException(Exception exception = null) 
            : base("Malformed IntuneWin file", exception)
        {
        }
    }
}