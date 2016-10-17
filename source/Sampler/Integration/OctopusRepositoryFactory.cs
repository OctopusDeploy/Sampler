using System;
using Octopus.Client;

namespace Octopus.Sampler.Integration
{
    class OctopusRepositoryFactory : IOctopusRepositoryFactory
    {
#pragma warning disable 618
        public IOctopusRepository CreateRepository(OctopusServerEndpoint endpoint)
        {
            return new OctopusRepository(endpoint);
        }
#pragma warning restore 618
    }
}