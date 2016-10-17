using System;

namespace Octopus.Sampler.Infrastructure
{
    public interface ICommandMetadata
    {
        string Name { get; }
        string[] Aliases { get; }
        string Description { get; }
    }
}