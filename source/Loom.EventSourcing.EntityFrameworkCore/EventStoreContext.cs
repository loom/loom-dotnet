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

// TODO: Use nullable-reference in C# 8.0 and remove the following preprocessor.
#pragma warning disable CA1062 // Validate arguments of public methods
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureStreamEventEntity(modelBuilder.Entity<StreamEvent>());
        }
#pragma warning restore CA1062 // Validate arguments of public methods

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
