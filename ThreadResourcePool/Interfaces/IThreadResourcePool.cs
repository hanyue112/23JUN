using System.Collections.Generic;
using System.Threading;

namespace ThreadResourcePool
{
    public interface IThreadResourcePool
    {
        void QueueUserWorkItem(WaitCallback callBack, object state, List<string> request);
        bool AddResource(string name);
    }
}
