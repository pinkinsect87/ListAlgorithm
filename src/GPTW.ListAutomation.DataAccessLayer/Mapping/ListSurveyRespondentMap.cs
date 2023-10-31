using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListSurveyRespondent mapping configuration
    /// </summary>
    public partial class ListSurveyRespondentMap : MappingConfiguration<ListSurveyRespondent>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListSurveyRespondent> builder)
        {
            builder.ToTable("ListSurveyRespondent");
            builder.HasKey(e => e.ListCompanyResponseId);

            builder.HasOne(d => d.ListCompany)
                .WithMany(p => p.ListSurveyRespondents)
                .HasForeignKey(d => d.ListCompanyId);

            base.Configure(builder);
        }
    }
}
