using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListRequest mapping configuration
    /// </summary>
    public partial class ListRequestMap : MappingConfiguration<ListRequest>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListRequest> builder)
        {
            builder.ToTable("ListRequest");
            builder.HasKey(e => e.ListRequestId);

            builder.Property(e => e.AffiliateId)
                    .HasMaxLength(10)
                    .IsUnicode(false);

            builder.Property(e => e.CountryCode)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(e => e.CreateDateTime).HasColumnType("datetime");

            builder.Property(e => e.CreatedBy)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(e => e.ListName)
                .HasMaxLength(5000)
                .IsUnicode(false);

            builder.Property(e => e.ListNameLocalLanguage)
                .HasMaxLength(5000)
                .IsUnicode(false);

            builder.Property(e => e.ModifiedBy)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(e => e.ModifiedDateTime).HasColumnType("datetime");

            builder.HasOne(d => d.Affiliate)
                .WithMany(p => p.ListRequests)
                .HasForeignKey(d => d.AffiliateId);

            builder.HasOne(d => d.AlgorithProcessedStatus)
                .WithMany()
                .HasForeignKey(d => d.AlgorithProcessedStatusId);

            builder.HasOne(d => d.Template)
                .WithMany(p => p.ListRequests)
                .HasForeignKey(d => d.TemplateId);

            builder.HasOne(d => d.UploadStatus)
                .WithMany()
                .HasForeignKey(d => d.UploadStatusId);

            base.Configure(builder);
        }
    }
}
