using Microsoft.EntityFrameworkCore;
using Digital_Wallet_System.Models;

namespace Digital_Wallet_System.Data
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<TransferTransaction> TransferTransactions { get; set; }
        public DbSet<DepositTransaction> DepositTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make username unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            
            // Make the email unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Wallet foreign key to User
            modelBuilder.Entity<User>()
                .HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallet>(w => w.UserId);
            
            // Transaction relationships
            modelBuilder.Entity<TransferTransaction>()
                .HasOne(t => t.Sender)
                .WithMany()
                .HasForeignKey(t => t.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferTransaction>()
                .HasOne(t => t.Recipient)
                .WithMany()
                .HasForeignKey(t => t.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Deposit-Transaction relationships
            modelBuilder.Entity<DepositTransaction>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Specifying the precision and scale for the Balance property
            modelBuilder.Entity<Wallet>()
                .Property(w => w.Balance)
                .HasColumnType("decimal(18,2)");

            // Specifying the precision and scale for the Amount property
            modelBuilder.Entity<TransferTransaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");
            
            // Specifying the precision and scale for the Amount property
            modelBuilder.Entity<DepositTransaction>()
                .Property(t => t.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}