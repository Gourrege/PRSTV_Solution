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
    public class CandidateService
    {
        private readonly Client _client;

        public CandidateService(Client client)
        {
            _client = client;
        }

        public async Task<List<Candidate>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var distinct = ids.Distinct().ToList();
            if (distinct.Count == 0) return new List<Candidate>();

            // PostgREST expects: in.(1,2,3)
            var inValue = $"({string.Join(",", distinct)})";

            var resp = await _client
                .From<Candidate>()
                .Filter("id", Operator.In, distinct)
                .Get();

            return resp.Models ?? new List<Candidate>();
        }

        public async Task<List<Candidate>> GetAllAsync()
        {
            var resp = await _client
                .From<Candidate>()
                .Order(x => x.Id, Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            return resp.Models ?? new List<Candidate>();
        }
    }
}
