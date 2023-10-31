using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListCompany mapping configuration
    /// </summary>
    public partial class ListCompanyMap : MappingConfiguration<ListCompany>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListCompany> builder)
        {
            builder.ToTable("ListCompany");
            builder.HasKey(e => e.ListCompanyId);

            builder.Property(e => e.CertificationDateTime).HasColumnType("datetime");

            builder.Property(e => e.ClientName).IsUnicode(false);

            builder.Property(e => e.SurveyDateTime).HasColumnType("datetime");

            builder.Property(e => e.SurveyVersionId)
                .HasMaxLength(50)
                .IsUnicode(false);

            builder.HasOne(d => d.ListRequest)
                .WithMany(p => p.ListCompanies)
                .HasForeignKey(d => d.ListRequestId);

            builder.HasOne(d => d.ListSourceFile)
                .WithMany(p => p.ListCompanies)
                .HasForeignKey(d => d.ListSourceFileId);

            base.Configure(builder);
        }
    }
}
