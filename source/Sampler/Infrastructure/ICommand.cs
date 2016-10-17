using System;
using System.IO;

namespace Octopus.Sampler.Infrastructure
{
    public interface ICommand
    {
        void GetHelp(TextWriter writer);
        void Execute(string[] commandLineArguments);
    }
}