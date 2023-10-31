using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a Country mapping configuration
    /// </summary>
    public partial class CountryMap : MappingConfiguration<Country>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Country> builder)
        {
            builder.ToTable("Countries");
            builder.HasKey(e => e.CountryCode);

            builder.Property(e => e.CountryCode)
                     .HasMaxLength(50)
                     .IsUnicode(false);

            builder.Property(e => e.AffiliateId)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.Property(e => e.CountryName)
                .IsRequired()
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.HasOne(d => d.Affiliate)
                .WithMany(p => p.Countries)
                .HasForeignKey(d => d.AffiliateId);

            base.Configure(builder);
        }
    }
}
