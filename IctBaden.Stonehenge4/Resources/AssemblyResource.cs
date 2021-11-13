﻿// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using System.Reflection;

namespace IctBaden.Stonehenge4.Resources
{
    internal class AssemblyResource
    {
        public string FullName { get; private set; }

        public string ShortName { get; private set; }

        public Assembly Assembly { get; private set; }

        public AssemblyResource(string fullName, string shortName, Assembly assembly)
        {
            FullName = fullName;
            ShortName = shortName;
            Assembly = assembly;
        }
    }
} 
