using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListCompanyCultureBrief mapping configuration
    /// </summary>
    public partial class ListCompanyCultureBriefMap : MappingConfiguration<ListCompanyCultureBrief>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListCompanyCultureBrief> builder)
        {
            builder.ToTable("ListCompanyCultureBrief");
            builder.HasKey(e => e.ListCompanyCultureBriefId);

            builder.Property(e => e.VariableName)
                    .IsRequired()
                    .IsUnicode(false);

            builder.Property(e => e.VariableValue)
                .IsRequired()
                .IsUnicode(false);

            builder.HasOne(d => d.ListCompany)
                .WithMany(p => p.ListCompanyCultureBriefs)
                .HasForeignKey(d => d.ListCompanyId);

            base.Configure(builder);
        }
    }
}
