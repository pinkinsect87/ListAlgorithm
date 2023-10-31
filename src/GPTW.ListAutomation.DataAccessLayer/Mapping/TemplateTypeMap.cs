using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a TemplateType mapping configuration
    /// </summary>
    public partial class TemplateTypeMap : MappingConfiguration<TemplateType>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<TemplateType> builder)
        {
            builder.ToTable("TemplateType");
            builder.HasKey(e => e.TemplateTypeId);

            builder.Property(e => e.CreatedBy)
                    .HasMaxLength(100)
                    .IsUnicode(false);

            builder.Property(e => e.CreatedDateTime).HasColumnType("datetime");

            builder.Property(e => e.ModifiedBy)
                .HasMaxLength(100)
                .IsUnicode(false);

            builder.Property(e => e.ModifiedDateTime).HasColumnType("datetime");

            builder.Property(e => e.TemplateTypeDescription)
                .HasMaxLength(5000)
                .IsUnicode(false);

            builder.Property(e => e.TemplateTypeName)
                .HasMaxLength(500)
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
