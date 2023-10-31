using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a Blsdata mapping configuration
    /// </summary>
    public partial class BlsdataMap : MappingConfiguration<Blsdata>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Blsdata> builder)
        {
            builder.ToTable("BLSData");
            builder.HasKey(e => e.BlsdataId);

            builder.Property(e => e.BlsdataId)
                .HasColumnName("BLSDataId")
                .ValueGeneratedOnAdd();

            builder.Property(e => e.BlsdataKey)
                .IsRequired()
                .HasColumnName("BLSDataKey")
                .HasMaxLength(200);

            builder.Property(e => e.BlsdataValue)
                .IsRequired()
                .HasColumnName("BLSDataValue")
                .HasMaxLength(200);

            base.Configure(builder);
        }
    }
}
