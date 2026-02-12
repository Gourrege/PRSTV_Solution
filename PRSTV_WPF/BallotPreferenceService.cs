using PRSTV_WPF.Models;
using Supabase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Supabase.Postgrest.Constants;

namespace PRSTV_WPF
{
    public class BallotPreferenceService
    {
        private readonly Client _client;

        public BallotPreferenceService(Client client)
        {
            _client = client;
        }

        public async Task<List<BallotPreference>> GetByRandomBallotIdAsync(long randomBallotId)
        {
            var resp = await _client
                .From<BallotPreference>()
                .Filter("random_ballot_id", Operator.Equals, randomBallotId.ToString())
                .Order(x => x.Preference, Ordering.Ascending)
                .Get();

            return resp.Models ?? new List<BallotPreference>();
        }

        /// <summary>
        /// Updates Preference values by row id (one UPDATE per row).
        /// </summary>
        public async Task UpdatePreferencesAsync(IEnumerable<BallotPreference> prefs)
        {
            var tasks = prefs.Select(p =>
                _client
                    .From<BallotPreference>()
                    .Where(x => x.Id == p.Id)
                    .Set(x => x.Preference, p.Preference)
                    .Update()
            );

            await Task.WhenAll(tasks);
        }

        public async Task ApplyPreferenceChangesAsync(List<BallotPreference> toInsert, List<BallotPreference> toUpdate, List<long> toDeleteIds)
        {
            var tasks = new List<Task>();

            // deletes
            foreach (var id in toDeleteIds)
            {
                tasks.Add(_client
                    .From<BallotPreference>()
                    .Where(x => x.Id == id)
                    .Delete());
            }

            // updates
            foreach (var p in toUpdate)
            {
                tasks.Add(_client
                    .From<BallotPreference>()
                    .Where(x => x.Id == p.Id)
                    .Set(x => x.Preference, p.Preference)
                    .Update());
            }

            // inserts
            if (toInsert.Count > 0)
            {
                tasks.Add(_client
                    .From<BallotPreference>()
                    .Insert(toInsert));
            }

            await Task.WhenAll(tasks);
        }
    }
}
