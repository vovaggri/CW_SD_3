using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> opts)
            : base(opts) { }

        public DbSet<Account>       Accounts => Set<Account>();
        public DbSet<InboxMessage>  Inbox    => Set<InboxMessage>();
        public DbSet<OutboxMessage> Outbox   => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>()
                .HasKey(a => a.UserId);

            modelBuilder.Entity<Account>()
                .Property(a => a.Balance)
                .HasPrecision(18, 2);
        }
    }
}