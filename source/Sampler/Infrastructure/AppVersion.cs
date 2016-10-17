using System;
using System.Reflection;
using Octopus.Sampler.Extensions;

namespace Octopus.Sampler.Infrastructure
{
    public class AppVersion
    {
        readonly SemanticVersionInfo semanticVersionInfo;

        public AppVersion(Assembly assembly)
            : this(assembly.GetSemanticVersionInfo())
        {
        }

        public AppVersion(SemanticVersionInfo semanticVersionInfo)
        {
            this.semanticVersionInfo = semanticVersionInfo;
        }

        public override string ToString()
        {
            return semanticVersionInfo.NuGetVersion;
        }
    }
}