﻿using System;
using Octopus.Client;

namespace Octopus.Sampler.Integration
{
    class OctopusRepositoryFactory : IOctopusRepositoryFactory
    {
        public IOctopusRepository CreateRepository(OctopusServerEndpoint endpoint)
        {
            return new OctopusRepository(endpoint);
        }
    }
}