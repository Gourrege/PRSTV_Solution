using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;

namespace PRSTV_WPF
{
    public class SupabaseService
    {
        public Client Client { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            if (Client != null) return;

            var options = new SupabaseOptions
            {
                AutoConnectRealtime = false // set true only if you need realtime
            };

            Client = new Client(SupabaseConfig.Url, SupabaseConfig.AnonKey, options);

            // Must be called before using From<T>(), Auth, etc.
            await Client.InitializeAsync();
        }
    }
}
