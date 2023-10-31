using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a Statement mapping configuration
    /// </summary>
    public partial class StatementMap : MappingConfiguration<Statement>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Statement> builder)
        {
            builder.ToTable("Statement");
            builder.HasNoKey();

            builder.Property(e => e.CreatedBy)
                    .HasMaxLength(500)
                    .IsUnicode(false);

            builder.Property(e => e.CreatedDate).HasColumnType("datetime");

            builder.Property(e => e.ModifiedBy)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.ModifiedDate).HasColumnType("datetime");

            builder.Property(e => e.StatementName)
                .IsRequired()
                .HasColumnName("Statement")
                .IsUnicode(false);

            base.Configure(builder);
        }
    }
}
