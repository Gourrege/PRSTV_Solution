using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL; // ensure Npgsql EFCore package
using PRSTV_ClassLibrary.SupabaseTbls;

namespace PRSTV_ConsoleApp.Data
{
    public class RawBallotDbContext : DbContext
    {
        public RawBallotDbContext(DbContextOptions<RawBallotDbContext> options) : base(options) { }
        public RawBallotDbContext() { } // optional for runtime, but factory is what tooling uses

        public DbSet<BallotPaperEF> BallotPapers => Set<BallotPaperEF>();
        public DbSet<BallotPreferenceEF> BallotPreferences => Set<BallotPreferenceEF>();
        public DbSet<CandidateEF> Candidates => Set<CandidateEF>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured) return;
            
            Env.TraversePath().Load();

            var connStr = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNSTR")
                          ?? throw new Exception("Missing SUPABASE_DB_CONNSTR env var.");
            options.UseNpgsql(connStr, o =>
            {
                o.MapEnum<BALLOT_STATE>("ballot_state");
            });
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Register the Postgres enum type, so EF sends/compares correctly
            modelBuilder.HasPostgresEnum<BALLOT_STATE>();

            modelBuilder.Entity<BallotPaperEF>()
                .Property(b => b.BallotState)
                .HasColumnType("ballot_state"); // matches DB enum type name

            modelBuilder.Entity<BallotPaperEF>()
                .HasIndex(b => b.RandomBallotId)
                .IsUnique();
            
            
            modelBuilder.Entity<BallotPreferenceEF>()
                .HasIndex(p => new { p.RandomBallotId, p.Preference });

            modelBuilder.Entity<BallotPreferenceEF>()
                .HasOne(p => p.BallotPaper)
                .WithMany(b => b.Preferences)
                .HasPrincipalKey(b => b.RandomBallotId)
                .HasForeignKey(p => p.RandomBallotId);


            modelBuilder.Entity<BallotPreferenceEF>()
            .HasOne(p => p.Candidate)
            .WithMany(c => c.Preferences)
            .HasForeignKey(p => p.CandidateId);
        }
    }
}