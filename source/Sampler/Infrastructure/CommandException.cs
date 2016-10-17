using System;

namespace Octopus.Sampler.Infrastructure
{
    public class CommandException : Exception
    {
        public CommandException(string message)
            : base(message)
        {
        }
    }
}