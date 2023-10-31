using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents an Affiliate mapping configuration
    /// </summary>
    public partial class AffiliateMap : MappingConfiguration<Affiliate>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Affiliate> builder)
        {
            builder.ToTable("Affiliates");
            builder.HasKey(e => e.AffiliateId);

            builder.Property(e => e.AffiliateId)
                    .HasMaxLength(10)
                    .IsUnicode(false);

            builder.Property(e => e.AffiliateName)
                .IsRequired()
                .HasMaxLength(5000)
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
