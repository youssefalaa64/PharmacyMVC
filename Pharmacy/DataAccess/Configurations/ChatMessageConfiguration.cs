using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pharmacy.DataAccess.Configurations
{
    public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(
            EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable("ChatMessages");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(m => m.SenderId)
                .IsRequired();

            builder.Property(m => m.SentAt)
                .IsRequired();

            builder.Property(m => m.IsRead)
                .HasDefaultValue(false);

            builder.HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => m.ChatId);

            builder.HasIndex(m => new
            {
                m.ChatId,
                m.SentAt
            });
        }
    }
}
