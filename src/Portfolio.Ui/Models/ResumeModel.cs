using System.Text.RegularExpressions;

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

public static partial class ResumeModel
{
    private static string? _enCvFileName;
    private static string? _trCvFileName;

    [GeneratedRegex(@"^Anil Sezer CV v[\d.]+ - (?<lang>EN|TR)\.pdf$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    public static partial Regex CvFileNameRegex();

    public static void ValidateAndLoadCvFiles(string webRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webRootPath);

        var cvDirectory = Path.Combine(webRootPath, "cv");
        if (!Directory.Exists(cvDirectory))
            throw new DirectoryNotFoundException($"CV directory was not found at '{cvDirectory}'.");

        var fileNames = Directory.GetFiles(cvDirectory)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();

        var enMatches = new List<string>();
        var trMatches = new List<string>();

        foreach (var fileName in fileNames)
        {
            var match = CvFileNameRegex().Match(fileName);
            if (!match.Success)
                continue;

            var lang = match.Groups["lang"].Value;
            if (lang.Equals("EN", StringComparison.OrdinalIgnoreCase))
                enMatches.Add(fileName);
            else if (lang.Equals("TR", StringComparison.OrdinalIgnoreCase))
                trMatches.Add(fileName);
        }

        if (enMatches.Count != 1)
            throw new InvalidOperationException($"Expected exactly 1 English CV matching pattern in '{cvDirectory}', but found {enMatches.Count}.");

        if (trMatches.Count != 1)
            throw new InvalidOperationException($"Expected exactly 1 Turkish CV matching pattern in '{cvDirectory}', but found {trMatches.Count}.");

        _enCvFileName = enMatches[0];
        _trCvFileName = trMatches[0];
    }

    public static string GetCvFileName(bool isTurkish)
    {
        var fileName = isTurkish ? _trCvFileName : _enCvFileName;
        if (string.IsNullOrEmpty(fileName))
            throw new InvalidOperationException($"CV filenames have not been initialized. Ensure {nameof(ValidateAndLoadCvFiles)} is called at startup.");

        return fileName;
    }

    public static WorkExperienceModel GetActiveRole()
    {
        return new WorkExperienceModel
        {
            RoleTitleKey = "Resume_CurrentJob_Title",
            CompanyKey = "Resume_CurrentJob_Company",
            DateKey = "Resume_CurrentJob_Date",
            BulletKeys = [
                "Resume_CurrentJob_Bullet1",
                "Resume_CurrentJob_Bullet2",
                "Resume_CurrentJob_Bullet3",
                "Resume_CurrentJob_Bullet4"
            ],
            TechTags = [
                "Resume_Tag_Docker",
                "Resume_Tag_DotNet10",
                "Resume_Tag_Flutter",
                "Resume_Tag_Php",
                "Resume_Tag_IoT",
                "Resume_Tag_Tdd",
                "Resume_Tag_EventBus",
                "Resume_Tag_Postgres",
                "Resume_Tag_MySql"
            ],
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
                TechTags = [
                    "Resume_Tag_Docker",
                    "Resume_Tag_Kubernetes",
                    "Resume_Tag_Ansible",
                    "Resume_Tag_Prometheus",
                    "Resume_Tag_Grafana",
                    "Resume_Tag_Traefik",
                    "Resume_Tag_Authentik"
                ],
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
                    "Resume_Job2_Bullet3"
                ],
                TechTags = [
                    "Resume_Tag_DotNet",
                    "Resume_Tag_CSharp",
                    "Resume_Tag_MsSql",
                    "Resume_Tag_Postgres",
                    "Resume_Tag_Kubernetes"
                ],
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
                TechTags = [
                    "Resume_Tag_Php",
                    "Resume_Tag_DotNet",
                    "Resume_Tag_Blazor",
                    "Resume_Tag_Kubernetes",
                    "Resume_Tag_Aws",
                    "Resume_Tag_Docker",
                    "Resume_Tag_ApacheKafka",
                    "Resume_Tag_MySql",
                    "Resume_Tag_Tdd",
                    "Resume_Tag_Ddd",
                    "Resume_Tag_EventBus"
                ],
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
                Year = "2023"
            },
            new()
            {
                TitleKey = "Resume_DomainDrivenDesign_Title",
                SubtitleKey = "Resume_DomainDrivenDesign_Author",
                Year = "2023"
            },
            new()
            {
                TitleKey = "Resume_Edu_Cs50_Title",
                SubtitleKey = "Resume_Cs50_SchoolAndTeacherName",
                Year = "2021"
            },
            new()
            {
                TitleKey = "Resume_DockerAToZ_Title",
                SubtitleKey = "Resume_Edu2_SchoolAndTeacherName",
                Year = "2021"
            },
            new()
            {
                TitleKey = "Resume_PluralSight_Title",
                SubtitleKey = "Resume_PluralSight_OrgName",
                Year = "2020"
            },
            new()
            {
                TitleKey = "Resume_CLanguage_OrgName",
                SubtitleKey = "Resume_Edu5_Title",
                Year = "2017"
            },
            new()
            {
                TitleKey = "Resume_Cpp_Title",
                SubtitleKey = "Resume_Cpp_Author",
                Year = "2015"
            },
            new()
            {
                TitleKey = "Resume_University_Title",
                SubtitleKey = "Resume_University_Major",
                Year = "2013 - 2018"
            },
            new()
            {
                TitleKey = "Resume_HighSchool_Title",
                SubtitleKey = "Resume_HighSchool_Speciality",
                Year = "2007 - 2011"
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
