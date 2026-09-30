using Xunit;

using CurriculumGenerator.Entities;
using FluentValidation.TestHelper;
using CurriculumGenerator.Validators;

namespace CurriculumGenerator.Tests;

public class CurriculumValidatorTests
{
    private readonly CurriculumValidator _validator = new();

    private static Curriculum ValidCurriculum() => new()
    {
        FullName = "Maria da Silva",
        Email = "maria@example.com",
        Experiences =
        [
            new Experience
            {
                CompanyName = "Acme",
                JobTitle = "Dev",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2022, 1, 1)
            }
        ],
        EducationList =
        [
            new Education
            {
                Course = "CS",
                InstitutionName = "UFMG",
                StartDate = new DateTime(2015, 1, 1),
                EndDate = new DateTime(2019, 1, 1)
            }
        ],
        Languages = [new Language { Name = "Portuguese", Proficiency = "Native" }]
    };

    [Fact]
    public void ValidCurriculum_Passes()
    {
        var result = _validator.TestValidate(ValidCurriculum());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MissingFullName_Fails()
    {
        var curriculum = ValidCurriculum();
        curriculum.FullName = "";
        var result = _validator.TestValidate(curriculum);
        result.ShouldHaveValidationErrorFor(c => c.FullName);
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var curriculum = ValidCurriculum();
        curriculum.Email = "not-an-email";
        var result = _validator.TestValidate(curriculum);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void EndDateBeforeStartDate_Fails()
    {
        var curriculum = ValidCurriculum();
        curriculum.Experiences[0].EndDate = curriculum.Experiences[0].StartDate!.Value.AddDays(-1);
        var result = _validator.TestValidate(curriculum);
        result.ShouldHaveValidationErrorFor("Experiences[0].EndDate");
    }

    [Fact]
    public void ExperienceWithoutCompany_Fails()
    {
        var curriculum = ValidCurriculum();
        curriculum.Experiences[0].CompanyName = "";
        var result = _validator.TestValidate(curriculum);
        result.ShouldHaveValidationErrorFor("Experiences[0].CompanyName");
    }

    [Fact]
    public void EducationWithoutCourse_Fails()
    {
        var curriculum = ValidCurriculum();
        curriculum.EducationList[0].Course = "";
        var result = _validator.TestValidate(curriculum);
        result.ShouldHaveValidationErrorFor("EducationList[0].Course");
    }
}
