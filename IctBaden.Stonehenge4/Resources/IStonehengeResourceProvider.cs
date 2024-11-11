using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.Hosting;

namespace IctBaden.Stonehenge.Resources;

public interface IStonehengeResourceProvider : IDisposable
{
    void InitProvider(StonehengeResourceLoader loader, StonehengeHostOptions options);
       
    IList<ViewModelInfo> GetViewModelInfos();

    Task<Resource?> Get(AppSession? session, CancellationToken requestAborted, string resourceName, IDictionary<string, string> parameters);
    Task<Resource?> Post(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData);
    Task<Resource?> Put(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData);
    Task<Resource?> Delete(AppSession? session, string resourceName, IDictionary<string, string> parameters, IDictionary<string, string> formData);
}