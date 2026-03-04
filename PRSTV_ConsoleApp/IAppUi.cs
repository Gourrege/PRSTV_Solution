using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_ConsoleApp
{
    public interface IAppUi
    {
        Task<string?> PromptAsync(string message);
        void Log(string message);
    }
}
