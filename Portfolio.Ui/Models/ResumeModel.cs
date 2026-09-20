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

public class CredentialModel
{
    public required string TitleKey { get; init; }
    public required string SubtitleKey { get; init; }
    public required string Year { get; init; }
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
            BulletKeys = ["Resume_CurrentJob_Bullet1"],
            TechTags = ["Resume_Tag_Management", "Resume_Tag_IoT", "Resume_Tag_Backend", "Resume_Tag_AppDevelopment", "Resume_Tag_DevOps"],
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
                BulletKeys =
                [
                    "Resume_Job1_Bullet1"
                ],
                TechTags = ["Resume_Tag_Docker", "Resume_Tag_VpsAndLinux", "Resume_Tag_SelfHosting", "Resume_Tag_DevOps", "Resume_Tag_Infrastructure"],
                IsActive = false,
                IsEarlyCareer = false
            },
            new()
            {
                RoleTitleKey = "Resume_Job2_Title",
                CompanyKey = "Resume_Job2_Company",
                DateKey = "Resume_Job2_Date",
                BulletKeys =
                [
                    "Resume_Job2_Bullet1",
                    "Resume_Job2_Bullet2",
                    "Resume_Job2_Bullet3",
                    "Resume_Job2_Bullet4"
                ],
                TechTags = ["Resume_Tag_DotNetCore", "Resume_Tag_CSharp", "Resume_Tag_Kubernetes", "Resume_Tag_Microservices", "Resume_Tag_Fintech", "Resume_Tag_Security"],
                IsActive = false,
                IsEarlyCareer = false
            },
            new()
            {
                RoleTitleKey = "Resume_Job3_Title",
                CompanyKey = "Resume_Job3_Company",
                DateKey = "Resume_Job3_Date",
                BulletKeys =
                [
                    "Resume_Job3_Bullet1",
                    "Resume_Job3_Bullet2",
                    "Resume_Job3_Bullet3",
                    "Resume_Job3_Bullet4"
                ],
                TechTags = ["Resume_Tag_CSharp", "Resume_Tag_DotNet", "Resume_Tag_ApacheKafka", "Resume_Tag_MultiTenancy", "Resume_Tag_HighAvailability"],
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
                BulletKeys = ["Resume_Job4_Bullet1"],
                TechTags = ["Resume_Tag_Banking", "Resume_Tag_CustomerService", "Resume_Tag_EnglishSupport"],
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Job5_Title",
                CompanyKey = "Resume_Job5_Company",
                DateKey = "Resume_Job5_Date",
                BulletKeys = ["Resume_Job5_Bullet1"],
                TechTags = ["Resume_Tag_Operations", "Resume_Tag_TeamSupervision", "Resume_Tag_ITSupport"],
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Job6_Title",
                CompanyKey = "Resume_Job6_Company",
                DateKey = "Resume_Job6_Date",
                BulletKeys = ["Resume_Job6_Bullet1"],
                TechTags = ["Resume_Tag_GuidedTours", "Resume_Tag_PublicCommunication"],
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Supervisor_Title",
                CompanyKey = "Resume_Supervisor_Company",
                DateKey = "Resume_Supervisor_Date",
                BulletKeys = ["Resume_Supervisor_Bullet1"],
                TechTags = [],
                IsActive = false,
                IsEarlyCareer = true
            },
            new()
            {
                RoleTitleKey = "Resume_Job_ItIntern_Title",
                CompanyKey = "Resume_ItIntern_Company",
                DateKey = "Resume_ItIntern_Date",
                BulletKeys = ["Resume_ItIntern_Bullet1", "Resume_ItIntern_Bullet2"],
                TechTags = [],
                IsActive = false,
                IsEarlyCareer = true
            }
        };
    }

    public static IReadOnlyList<CredentialModel> GetCredentials()
    {
        return new List<CredentialModel>
        {
            new()
            {
                TitleKey = "Resume_KubernetesUpAndRunning_Title",
                SubtitleKey = "Resume_KubernetesUpAndRunning_Authors",
                Year = "2023",
            },
            new()
            {
                TitleKey = "Resume_DomainDrivenDesign_Title",
                SubtitleKey = "Resume_DomainDrivenDesign_Author",
                Year = "2023",
            },
            new()
            {
                TitleKey = "Resume_Edu_Cs50_Title",
                SubtitleKey = "Resume_Cs50_SchoolAndTeacherName",
                Year = "2021",
            },
            new()
            {
                TitleKey = "Resume_DockerAToZ_Title",
                SubtitleKey = "Resume_Edu2_SchoolAndTeacherName",
                Year = "2021",
            },
            new()
            {
                TitleKey = "Resume_PluralSight_Title",
                SubtitleKey = "Resume_PluralSight_OrgName",
                Year = "2020",
            },
            new()
            {
                TitleKey = "Resume_CLanguage_OrgName",
                SubtitleKey = "Resume_Edu5_Title",
                Year = "2017",
            },
            new()
            {
                TitleKey = "Resume_Cpp_Title",
                SubtitleKey = "Resume_Cpp_Author",
                Year = "2015",
            },
            new()
            {
                TitleKey = "Resume_University_Title",
                SubtitleKey = "Resume_University_Major",
                Year = "2013 - 2018",
            },
            new()
            {
                TitleKey = "Resume_HighSchool_Title",
                SubtitleKey = "Resume_HighSchool_Speciality",
                Year = "2007 - 2011",
            },
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
