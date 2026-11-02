using FluentAssertions;
using Portfolio.Ui.Models;

namespace Portfolio.Test.Ui;

public class ResumeModelTests
{
    [Theory]
    [InlineData("Anil Sezer CV v6.4 - EN.pdf", true, "EN")]
    [InlineData("Anil Sezer CV v6.4 - TR.pdf", true, "TR")]
    [InlineData("Anil Sezer CV v7.0 - EN.pdf", true, "EN")]
    [InlineData("Anil Sezer CV v10.2.1 - TR.pdf", true, "TR")]
    [InlineData("Anil Sezer CV - EN.pdf", false, "")]
    [InlineData("Anil Sezer CV v6.4 - DE.pdf", false, "")]
    [InlineData("Anil Sezer CV v6.4 - EN.docx", false, "")]
    public void CvFileNameRegex_MatchesExpectedFormat(string fileName, bool expectedMatch, string expectedLang)
    {
        var match = ResumeModel.CvFileNameRegex().Match(fileName);

        match.Success.Should().Be(expectedMatch);
        if (expectedMatch)
        {
            match.Groups["lang"].Value.Should().Be(expectedLang);
        }
    }

    [Fact]
    public void ValidateAndLoadCvFiles_WithValidPair_ResolvesBothLanguageFiles()
    {
        var tempWebRoot = Path.Combine(Path.GetTempPath(), $"portfolio-cv-test-{Guid.NewGuid():N}");
        var cvDir = Path.Combine(tempWebRoot, "cv");
        Directory.CreateDirectory(cvDir);

        try
        {
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.4 - EN.pdf"), "dummy-en");
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.4 - TR.pdf"), "dummy-tr");

            ResumeModel.ValidateAndLoadCvFiles(tempWebRoot);

            ResumeModel.GetCvFileName(isTurkish: false).Should().Be("Anil Sezer CV v6.4 - EN.pdf");
            ResumeModel.GetCvFileName(isTurkish: true).Should().Be("Anil Sezer CV v6.4 - TR.pdf");
        }
        finally
        {
            Directory.Delete(tempWebRoot, recursive: true);
        }
    }

    [Fact]
    public void ValidateAndLoadCvFiles_WhenDuplicateLanguageVersionExists_ThrowsInvalidOperationException()
    {
        var tempWebRoot = Path.Combine(Path.GetTempPath(), $"portfolio-cv-test-{Guid.NewGuid():N}");
        var cvDir = Path.Combine(tempWebRoot, "cv");
        Directory.CreateDirectory(cvDir);

        try
        {
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.3 - EN.pdf"), "old-en");
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.4 - EN.pdf"), "new-en");
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.4 - TR.pdf"), "new-tr");

            var act = () => ResumeModel.ValidateAndLoadCvFiles(tempWebRoot);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Expected exactly 1 English CV*");
        }
        finally
        {
            Directory.Delete(tempWebRoot, recursive: true);
        }
    }

    [Fact]
    public void ValidateAndLoadCvFiles_WhenLanguageFileMissing_ThrowsInvalidOperationException()
    {
        var tempWebRoot = Path.Combine(Path.GetTempPath(), $"portfolio-cv-test-{Guid.NewGuid():N}");
        var cvDir = Path.Combine(tempWebRoot, "cv");
        Directory.CreateDirectory(cvDir);

        try
        {
            File.WriteAllText(Path.Combine(cvDir, "Anil Sezer CV v6.4 - EN.pdf"), "only-en");

            var act = () => ResumeModel.ValidateAndLoadCvFiles(tempWebRoot);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Expected exactly 1 Turkish CV*");
        }
        finally
        {
            Directory.Delete(tempWebRoot, recursive: true);
        }
    }
}
