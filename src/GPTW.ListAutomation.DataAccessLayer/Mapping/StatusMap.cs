using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a Status mapping configuration
    /// </summary>
    public partial class StatusMap : MappingConfiguration<Status>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Status> builder)
        {
            builder.ToTable("Status");
            builder.HasKey(e => e.StatusId);

            builder.Property(e => e.StatusDescription)
                    .HasMaxLength(250)
                    .IsUnicode(false);

            builder.Property(e => e.StatusName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
