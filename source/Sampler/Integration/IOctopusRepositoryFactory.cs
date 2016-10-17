using System;
using Octopus.Client;

namespace Octopus.Sampler.Integration
{
    public interface IOctopusRepositoryFactory
    {
        IOctopusRepository CreateRepository(OctopusServerEndpoint endpoint);
    }
}
