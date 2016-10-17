﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace Octopus.Sampler.Infrastructure
{
    public interface ICommand
    {
        void GetHelp(TextWriter writer);
        Task Execute(string[] commandLineArguments);
    }
}