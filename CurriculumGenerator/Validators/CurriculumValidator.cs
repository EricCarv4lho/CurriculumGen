using CurriculumGenerator.Entities;
using FluentValidation;


namespace CurriculumGenerator.Validators
{
    public class CurriculumValidator : AbstractValidator<Curriculum>
    {

        public CurriculumValidator()
        {

            RuleFor(curriculum => curriculum.FullName)
                .NotNull()
                .NotEmpty()
                .WithMessage("The full name is required.");

        
            RuleFor(curriculum => curriculum.Email)
                .NotNull()
                .NotEmpty()
                .EmailAddress()
                .WithMessage("A valid email address is required.");

            RuleForEach(curriculum => curriculum.Experiences)
                .ChildRules(e =>
                {
                    e.RuleFor(e => e.EndDate).GreaterThan(e => e.StartDate).WithMessage("The end date must be greather than the start date.");
                    e.RuleFor(e => e.CompanyName).NotNull().NotEmpty().WithMessage("The company name is required");
                });


            RuleForEach(curriculum => curriculum.EducationList)
           .ChildRules(e =>
           {
               e.RuleFor(e => e.EndDate).GreaterThan(e => e.StartDate).WithMessage("The end date must be greather than the start date.");
               e.RuleFor(e => e.InstitutionName).NotNull().NotEmpty().WithMessage("The Institution name is required");
               e.RuleFor(e => e.Course).NotNull().NotEmpty().WithMessage("The course title is required");
           });

            RuleForEach(curriculum => curriculum.Languages)
        .ChildRules(l =>
        {
            l.RuleFor(l => l.Name).NotNull().NotEmpty().WithMessage("The Language is required");
        });

        }
    }
}
