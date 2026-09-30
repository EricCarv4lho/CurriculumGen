namespace CurriculumGenerator.Entities
{
    public class Education
    {
        public DateTime? StartDate { get; set; } = null;
        public DateTime? EndDate { get; set; } = null;
        public string Course { get; set; } = string.Empty;
        public string InstitutionName { get; set; } = string.Empty;
    }
}
