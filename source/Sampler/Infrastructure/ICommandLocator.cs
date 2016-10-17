using System;

namespace Octopus.Sampler.Infrastructure
{
    public interface ICommandLocator
    {
        ICommandMetadata[] List();
        ICommand Find(string name);
    }
}