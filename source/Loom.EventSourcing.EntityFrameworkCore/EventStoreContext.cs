namespace Loom.EventSourcing.EntityFrameworkCore
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class EventStoreContext : DbContext
    {
        public EventStoreContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<StreamEvent> StreamEvents { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureStreamEventEntity(modelBuilder.Entity<StreamEvent>());
        }

        private static void ConfigureStreamEventEntity(
            EntityTypeBuilder<StreamEvent> entity)
        {
            entity.HasKey(e => e.Sequence);
            entity.HasIndex(e => new { e.StreamId, e.Version }).IsUnique();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
        }
    }
}
