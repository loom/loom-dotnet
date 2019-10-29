namespace Loom.EventSourcing.EntityFrameworkCore
{
    using System;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class EventStoreContext : DbContext
    {
        public EventStoreContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<StreamEvent> StreamEvents { get; protected set; }

        public DbSet<PendingEvent> PendingEvents { get; protected set; }

        public DbSet<UniqueProperty> UniqueProperties { get; protected set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder is null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            ConfigureStreamEventEntity(modelBuilder.Entity<StreamEvent>());
            ConfigurePendingEventEntity(modelBuilder.Entity<PendingEvent>());
            ConfigureUniquePropertyEntity(modelBuilder.Entity<UniqueProperty>());
        }

        private static void ConfigureStreamEventEntity(EntityTypeBuilder<StreamEvent> entity)
        {
            entity.HasKey(e => e.Sequence);
            entity.HasIndex(e => new { e.StateType, e.StreamId, e.Version }).IsUnique();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
        }

        private static void ConfigurePendingEventEntity(EntityTypeBuilder<PendingEvent> entity)
        {
            entity.HasKey(e => new { e.StateType, e.StreamId, e.Version });
            entity.Ignore(e => e.TracingProperties);
        }

        private static void ConfigureUniquePropertyEntity(EntityTypeBuilder<UniqueProperty> entity)
        {
            entity.HasKey(e => e.Sequence);
            entity.HasIndex(e => new { e.StateType, e.Name, e.Value }).IsUnique();
            entity.HasIndex(e => new { e.StateType, e.StreamId, e.Name }).IsUnique();
            entity.Property(e => e.StateType).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(128);
            entity.Property(e => e.Value).HasMaxLength(256);
        }
    }
}
