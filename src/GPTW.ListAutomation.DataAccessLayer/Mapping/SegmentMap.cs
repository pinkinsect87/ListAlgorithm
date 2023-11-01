using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a Segment mapping configuration
    /// </summary>
    public partial class SegmentMap : MappingConfiguration<Segment>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Segment> builder)
        {
            builder.ToTable("Segment");
            builder.HasKey(e => e.SegmentId);

            builder.Property(e => e.SegmentName)
                .IsRequired()
                .HasMaxLength(200)
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
