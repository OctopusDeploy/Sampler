using System;
using Octopus.Client;

namespace Octopus.Sampler.Integration
{
    public interface IOctopusRepositoryFactory
    {
#pragma warning disable 618
        IOctopusRepository CreateRepository(OctopusServerEndpoint endpoint);
#pragma warning restore 618
    }
}
