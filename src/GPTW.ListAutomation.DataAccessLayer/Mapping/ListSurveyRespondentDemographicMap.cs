using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListSurveyRespondentDemographic mapping configuration
    /// </summary>
    public partial class ListSurveyRespondentDemographicMap : MappingConfiguration<ListSurveyRespondentDemographic>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListSurveyRespondentDemographic> builder)
        {
            builder.ToTable("ListSurveyRespondentDemographics");
            builder.HasKey(e => e.ListCompanyDemographicsId);

            builder.Property(e => e.Age)
                    .HasMaxLength(500)
                    .IsUnicode(false);

            builder.Property(e => e.BirthYear)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.Confidence)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.CountryRegion)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.CreatedDateTime).HasColumnType("datetime");

            builder.Property(e => e.Disabilities)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.Gender)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.JobLevel)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.LgbtOrLgbtQ)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.ManagerialLevel)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.MeaningfulInnovationOpportunities)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.ModifiedDateTime).HasColumnType("datetime");

            builder.Property(e => e.PayType)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.RaceEthniticity)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.Responsibility)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.Tenure)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.WorkStatus)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.WorkType)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.WorkerType)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.Property(e => e.Zipcode)
                .HasMaxLength(500)
                .IsUnicode(false);

            builder.HasOne(d => d.ListCompany)
                .WithMany(p => p.ListSurveyRespondentDemographics)
                .HasForeignKey(d => d.ListCompanyId);

            base.Configure(builder);
        }
    }
}
