using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pharmacy.DataAccess.Configurations
{
    public class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.ToTable("Chats");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.CustomerId)
                .IsRequired();

            builder.Property(c => c.AdminId)
                .IsRequired(false);

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.HasOne(c => c.Customer)
                .WithMany(u => u.CustomerChats)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Admin)
                .WithMany(u => u.AdminChats)
                .HasForeignKey(c => c.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Messages)
                .WithOne(m => m.Chat)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => c.CustomerId);

            builder.HasIndex(c => c.AdminId);
        }
    }
}
