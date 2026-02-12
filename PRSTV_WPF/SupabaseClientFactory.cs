using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;

namespace PRSTV_WPF
{
    public class SupabaseClientFactory
    {
        public static Client Client { get; private set; } = null!;

        public static async Task InitializeAsync()
        {
            if (Client != null) return;

            Client = new Client(
                SupabaseConfig.Url,
                SupabaseConfig.AnonKey,
                new SupabaseOptions { AutoConnectRealtime = false }
            );

            await Client.InitializeAsync();
        }
    }
}
