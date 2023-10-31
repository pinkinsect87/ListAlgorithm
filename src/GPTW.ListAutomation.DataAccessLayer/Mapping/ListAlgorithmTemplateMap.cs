using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListAlgorithmTemplate mapping configuration
    /// </summary>
    public partial class ListAlgorithmTemplateMap : MappingConfiguration<ListAlgorithmTemplate>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListAlgorithmTemplate> builder)
        {
            builder.ToTable("ListAlgorithmTemplate");
            builder.HasKey(e => e.TemplateId);

            builder.Property(e => e.ManifestFileInfo)
                    .HasMaxLength(500)
                    .IsUnicode(false);

            builder.Property(e => e.ManifestFileXml).HasColumnType("xml");

            builder.Property(e => e.TemplateName)
                .HasMaxLength(200)
                .IsUnicode(false);

            builder.Property(e => e.TemplateVersion)
                .IsRequired()
                .HasMaxLength(10)
                .IsUnicode(false);

            builder.HasOne(d => d.TemplateType)
                .WithMany(p => p.ListAlgorithmTemplates)
                .HasForeignKey(d => d.TemplateTypeId);

            base.Configure(builder);
        }
    }
}
