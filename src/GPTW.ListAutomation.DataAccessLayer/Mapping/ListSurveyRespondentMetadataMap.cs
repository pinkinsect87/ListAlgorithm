using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListSurveyRespondentMetadata mapping configuration
    /// </summary>
    public partial class ListSurveyRespondentMetadataMap : MappingConfiguration<ListSurveyRespondentMetadata>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListSurveyRespondentMetadata> builder)
        {
            builder.ToTable("ListSurveyRespondentMetadata"); 
            builder.HasNoKey();

            builder.Property(e => e.MetadataKey)
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);

            builder.Property(e => e.MetadataValue)
                .HasMaxLength(5000)
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
