namespace Portfolio.Ui.Models;

public class WorkExperienceModel
{
    public required string RoleTitleKey { get; init; }
    public required string CompanyKey { get; init; }
    public required string DateKey { get; init; }
    public required IReadOnlyList<string> BulletKeys { get; init; }
    public required IReadOnlyList<string> TechTags { get; init; }
    public bool IsActive { get; init; }
    public bool IsEarlyCareer { get; init; }
}

public class CertificationModel
{
    public required string TitleKey { get; init; }
    public required string IssuerKey { get; init; }
    public required string Year { get; init; }
}

public class EducationDegreeModel
{
    public required string InstitutionKey { get; init; }
    public required string DegreeKey { get; init; }
    public required string DateRange { get; init; }
}

public class ResumeStatModel
{
    public required string Value { get; init; }
    public required string LabelKey { get; init; }
}

public static class ResumeModel
{
    public const string CvDownloadUrl = "https://drive.google.com/drive/folders/1iBSn56NqtXe1OL55fs3-oqy53_Xf9veH?usp=sharing";

    public static WorkExperienceModel GetActiveRole()
    {
        return new WorkExperienceModel
        {
            RoleTitleKey = "Resume_CurrentJob_Title",
            CompanyKey = "Resume_CurrentJob_Company",
            DateKey = "Resume_CurrentJob_Date",
            BulletKeys = new[] { "Resume_CurrentJob_Bullet1" },
            TechTags = new[] { "Management", "IoT", "Full-stack development", "DevOps", "Infrastructure" },
            IsActive = true,
            IsEarlyCareer = false
        };
    }

    public static IReadOnlyList<WorkExperienceModel> GetCoreEngineeringRoles()
    {
        return new List<WorkExperienceModel>
        {
            new()
            {
                RoleTitleKey = "Resume_Job1_Title",
                CompanyKey = "Resume_Job1_Company",
                DateKey = "Resume_Job1_Date",
                BulletKeys = new[]
                {
                    "Resume_Job1_Bullet1",
                },
                TechTags = new[] { "Docker", "VPS & Linux", "Self-Hosting", "DevOps", "Infrastructure" },
                IsActive = false,
                IsEarlyCareer = false
            },
            new()
            {
                RoleTitleKey = "Resume_Job2_Title",
                CompanyKey = "Resume_Job2_Company",
                DateKey = "Resume_Job2_Date",
                BulletKeys = new[]
                {
                    "Resume_Job2_Bullet1",
                    "Resume_Job2_Bullet2",
                    "Resume_Job2_Bullet3",
                    "Resume_Job2_Bullet4"
                },
                TechTags = new[] { ".NET Core", "C#", "Kubernetes", "Microservices", "Fintech", "Security" },
                IsActive = false,
                IsEarlyCareer = false
            },
            new()
            {
                RoleTitleKey = "Resume_Job3_Title",
                CompanyKey = "Resume_Job3_Company",
                DateKey = "Resume_Job3_Date",
                BulletKeys = new[]
                {
                    "Resume_Job3_Bullet1",
                    "Resume_Job3_Bullet2",
                    "Resume_Job3_Bullet3",
                    "Resume_Job3_Bullet4"
                },
                TechTags = new[] { "C#", ".NET", "Apache Kafka", "Multi-Tenancy", "High Availability" },
                IsActive = false,
                IsEarlyCareer = false
            }
        };
    }

    public static IReadOnlyList<WorkExperienceModel> GetEarlierCareerRoles()
    {
        return new List<WorkExperienceModel>
        {
            new()
            {
                RoleTitleKey = "Resume_Job4_Title",
                CompanyKey = "Resume_Job4_Company",
                DateKey = "Resume_Job4_Date",
                BulletKeys = new[] { "Resume_Job4_Bullet1" },
                TechTags = new[] { "Banking", "Customer Service", "English Support" },
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Job5_Title",
                CompanyKey = "Resume_Job5_Company",
                DateKey = "Resume_Job5_Date",
                BulletKeys = new[] { "Resume_Job5_Bullet1" },
                TechTags = new[] { "Operations", "Team Supervision", "IT Support" },
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Job6_Title",
                CompanyKey = "Resume_Job6_Company",
                DateKey = "Resume_Job6_Date",
                BulletKeys = new[] { "Resume_Job6_Bullet1" },
                TechTags = new[] { "Guided Tours", "Public Communication" },
                IsActive = false,
                IsEarlyCareer = true
            }
        };
    }

    public static IReadOnlyList<CertificationModel> GetCertifications()
    {
        return new List<CertificationModel>
        {
            new()
            {
                TitleKey = "Resume_Edu1_Title",
                IssuerKey = "Resume_Edu1_School",
                Year = "2021",
            },
            new()
            {
                TitleKey = "Resume_Edu2_Title",
                IssuerKey = "Resume_Edu2_School",
                Year = "2021",
            },
            new()
            {
                TitleKey = "Resume_Edu3_Title",
                IssuerKey = "Resume_Edu3_School",
                Year = "2020",
            },
            new()
            {
                TitleKey = "Resume_Edu5_Degree",
                IssuerKey = "Resume_Edu5_Title",
                Year = "2017",
            }
        };
    }

    public static IReadOnlyList<EducationDegreeModel> GetAcademicDegrees()
    {
        return new List<EducationDegreeModel>
        {
            new()
            {
                InstitutionKey = "Resume_Edu4_Title",
                DegreeKey = "Resume_Edu4_Degree",
                DateRange = "2013 - 2018",
            },
            new()
            {
                InstitutionKey = "Resume_Edu6_Title",
                DegreeKey = "Resume_Edu6_Degree",
                DateRange = "2007 - 2011",
            }
        };
    }

    private static int CalculateYearsFrom(DateTime startDate)
    {
        var today = DateTime.UtcNow;
        var years = today.Year - startDate.Year;
        if (today < startDate.AddYears(years))
        {
            years--;
        }
        return Math.Max(0, years);
    }

    public static IReadOnlyList<ResumeStatModel> GetResumeStats()
    {
        var softwareYears = CalculateYearsFrom(new DateTime(2019, 5, 1));
        var kubernetesYears = CalculateYearsFrom(new DateTime(2022, 1, 1));
        var softSkillsYears = CalculateYearsFrom(new DateTime(2016, 1, 1));

        return new List<ResumeStatModel>
        {
            new() { Value = $"{softwareYears}", LabelKey = "Resume_Stat_Software" },
            new() { Value = $"{kubernetesYears}", LabelKey = "Resume_Stat_Kubernetes" },
            new() { Value = "2", LabelKey = "Resume_Stat_IoT" },
            new() { Value = $"{softSkillsYears}", LabelKey = "Resume_Stat_SoftSkills" }
        };
    }
}
