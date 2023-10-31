using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListAutomationResult mapping configuration
    /// </summary>
    public partial class ListAutomationResultMap : MappingConfiguration<ListAutomationResult>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListAutomationResult> builder)
        {
            builder.ToTable("ListAutomationResult");
            builder.HasKey(e => e.ListAutomationResultId);

            builder.Property(e => e.CalculatedDate).HasColumnType("datetime");

            builder.Property(e => e.CalculationNotes)
                .HasMaxLength(5000)
                .IsUnicode(false);

            builder.Property(e => e.CalculationStatus)
                .HasMaxLength(10)
                .IsFixedLength();

            builder.Property(e => e.InternalNotes).IsUnicode(false);

            builder.Property(e => e.ResultKey)
                .IsRequired()
                .HasMaxLength(5000)
                .IsUnicode(false);

            builder.Property(e => e.ResultValue).IsUnicode(false);

            builder.Property(e => e.Variation).HasColumnType("decimal(18, 2)");

            base.Configure(builder);
        }
    }
}
