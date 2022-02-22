using System;
using System.Collections.Generic;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;

namespace IctBaden.Stonehenge.Resources
{
    public interface IStonehengeResourceProvider : IDisposable
    {
        void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options);
       
        List<ViewModelInfo> GetViewModelInfos();

        Resource Get(AppSession session, string resourceName, Dictionary<string, string> parameters);

        Resource Post(AppSession session, string resourceName, Dictionary<string, string> parameters, Dictionary<string, string> formData);
    }
}
