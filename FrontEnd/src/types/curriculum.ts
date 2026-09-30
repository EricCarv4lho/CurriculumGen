export interface ExperienceData {
  companyName: string;
  jobTitle: string;
  startDate: string;
  endDate: string | null;
  description: string;
}

export interface EducationData {
  course: string;
  institutionName: string;
  startDate: string;
  endDate: string | null;
}

export interface LanguageData {
  name: string;
  proficiency: string;
}

export interface CurriculumData {
  fullName: string;
  professionalTitle: string;
  city: string;
  state: string;
  phoneNumber: string;
  email: string;
  linkedinLink: string | null;
  gitHubLink: string | null;
  careerObjective: string;
  highlights: string;
  skillSet: string[];
  template: string;
  experiences: ExperienceData[];
  educationList: EducationData[];
  languages: LanguageData[];
}
