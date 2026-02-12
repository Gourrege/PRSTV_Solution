using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PRSTV_WPF.Models;
using Supabase;

namespace PRSTV_WPF
{
    public class BallotPaperService
    {
        private readonly Client _client;

        public BallotPaperService(Client client)
        {
            _client = client;
        }


        //Test for the connection and retrieval of data
        public async Task<List<BallotPaper>> GetFirst10Async()
        {
            var response = await _client
                .From<BallotPaper>()
                .Order(x => x.Id, Supabase.Postgrest.Constants.Ordering.Ascending)
                .Range(0, 1000)
                .Get();

            return response.Models;
        }

        // Retrieve all BallotPaper records in batches due to the limit of rows on a page in Supabase
        public async Task<List<BallotPaper>> GetAllAsync(int batchSize = 1000)
        {
            var all = new List<BallotPaper>();
            var start = 0;

            while (true)
            {
                var end = start + batchSize - 1;

                var resp = await _client
                    .From<BallotPaper>()
                    .Filter(x => x.BallotState, Supabase.Postgrest.Constants.Operator.Equals, "Doubtful")
                    .Order(x => x.Id, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Range(start, end)
                    .Get();

                var batch = resp.Models;

                if (batch == null || batch.Count == 0)
                    break;

                all.AddRange(batch);

                // If we received fewer than batchSize, we reached the end
                if (batch.Count < batchSize)
                    break;

                start += batchSize;
            }

            return all;
        }

        public async Task UpdateBallotStateAsync(long ballotId, BALLOT_SATE newState)
        {


            // Updates ballot_state for the row with matching id
            await _client
                .From<BallotPaper>()
                .Where(x => x.Id == ballotId)
                .Set(x => x.BallotState, newState.ToString())
                .Update();
        }
        




    }
}
