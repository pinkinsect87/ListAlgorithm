using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListSourceFile mapping configuration
    /// </summary>
    public partial class ListSourceFileMap : MappingConfiguration<ListSourceFile>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListSourceFile> builder)
        {
            builder.ToTable("ListSourceFile");
            builder.HasKey(e => e.ListSourceFileId);

            //builder.Property(e => e.FileType)
            //        .IsRequired()
            //        .HasMaxLength(50)
            //        .IsUnicode(false);

            //builder.Property(e => e.ModifiedBy)
            //    .HasMaxLength(100)
            //    .IsUnicode(false);

            builder.Property(e => e.ModifiedDateTime).HasColumnType("datetime");

            //builder.Property(e => e.UploadedBy)
            //    .HasMaxLength(100)
            //    .IsUnicode(false);

            builder.Property(e => e.UploadedDateTime).HasColumnType("datetime");

            builder.HasOne(d => d.ListRequest)
                .WithMany(p => p.ListSourceFiles)
                .HasForeignKey(d => d.ListRequestId);

            base.Configure(builder);
        }
    }
}
