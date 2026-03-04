using Microsoft.EntityFrameworkCore;
using PRSTV_ClassLibrary;
using PRSTV_ClassLibrary.CountClasses;
using PRSTV_ConsoleApp.LocalDB;
namespace PRSTV_ConsoleApp.Data
{
    public class CountContext : DbContext
    {
        public DbSet<CandidateCountState> Candidates => Set<CandidateCountState>();
        public DbSet<BallotCurrentState> BallotCurrentStates => Set<BallotCurrentState>();
        public DbSet<Election> Elections => Set<Election>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    @"Server=(localdb)\MSSQLLocalDB;Database=PRSTV_Local;Trusted_Connection=True;MultipleActiveResultSets=true"
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // CandidateCoutState (likely composite key per count run + candidate)
            modelBuilder.Entity<CandidateCountState>(entity =>
            {
                entity.ToTable("CandidateCountStates");

                entity.HasKey(e => e.CandidateCountStateId);


                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Surplus).IsRequired();
                entity.Property(e => e.TotalVotes).IsRequired();

                entity.Property(e => e.Name)
                      .HasMaxLength(200);

                //entity.HasIndex(e => e.CandidateCountStateId);
                entity.HasIndex(e => new { e.CountNumber, e.Status });
                entity.HasIndex(e => new { e.CountNumber, e.CandidateId });

                entity.HasOne(e => e.Election)
                  .WithMany(e => e.CandidateStates)
                  .HasForeignKey(e => e.ElectionId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            // BallotCurrentState
            modelBuilder.Entity<BallotCurrentState>(entity =>
            {
                entity.ToTable("BallotCurrentStates");

                entity.HasKey(e => e.BallotCurrentStateId);
                entity.HasIndex(e => new { e.CountNumber, e.RandomBallotId });
                entity.HasIndex(e => new { e.CountNumber, e.CurrentCandidateId, e.IsExhausted });

               // entity.Property(e => e.RandomBallotId).IsRequired();
                entity.Property(e => e.CountNumber).IsRequired();
                entity.Property(e => e.CurrentPreference).IsRequired();
                entity.Property(e => e.IsExhausted).IsRequired();

                entity.HasIndex(e => e.RandomBallotId);
                entity.HasIndex(e => new { e.CountNumber, e.CurrentCandidateId });


                entity.HasOne(e => e.Election)
                      .WithMany(e => e.BallotStates)
                      .HasForeignKey(e => e.ElectionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<Election>(entity =>
            {
                entity.ToTable("Elections");

                entity.HasKey(e => e.ElectionId);

                entity.Property(e => e.Seats).IsRequired();
                entity.Property(e => e.Quota).IsRequired();
                entity.Property(e => e.TotalValidPoll).IsRequired();
                entity.Property(e => e.CurrentCount).IsRequired();
                entity.Property(e => e.SeatsFilled).IsRequired();

                entity.HasIndex(e => e.CurrentCount);
            });
        }
    }
}
