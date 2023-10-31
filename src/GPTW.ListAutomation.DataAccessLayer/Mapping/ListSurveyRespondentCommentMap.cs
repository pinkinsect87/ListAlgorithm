using GPTW.ListAutomation.DataAccessLayer.Configuration;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GPTW.ListAutomation.DataAccessLayer.Mapping
{
    /// <summary>
    /// Represents a ListSurveyRespondentComment mapping configuration
    /// </summary>
    public partial class ListSurveyRespondentCommentMap : MappingConfiguration<ListSurveyRespondentComment>
    {
        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<ListSurveyRespondentComment> builder)
        {
            builder.ToTable("ListSurveyRespondentComments");
            builder.HasKey(e => e.ListSurveyRespondentCommentsId);

            builder.Property(e => e.Question).IsUnicode(false);

            builder.Property(e => e.Response).IsUnicode(false);

            builder.HasOne(d => d.ListCompany)
                .WithMany(p => p.ListSurveyRespondentComments)
                .HasForeignKey(d => d.ListCompanyId);

            base.Configure(builder);
        }
    }
}
