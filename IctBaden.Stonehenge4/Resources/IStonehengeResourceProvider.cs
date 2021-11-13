using System;
using System.Collections.Generic;
using IctBaden.Stonehenge4.Core;
using IctBaden.Stonehenge4.Hosting;

namespace IctBaden.Stonehenge4.Resources
{
    public interface IStonehengeResourceProvider : IDisposable
    {
        void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options);
        
        Resource Get(AppSession session, string resourceName, Dictionary<string, string> parameters);

        Resource Post(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData);
    }
}
