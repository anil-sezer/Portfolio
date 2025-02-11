// using FluentAssertions;
// using Microsoft.EntityFrameworkCore;
// using Portfolio.Domain.Entities;
// using Portfolio.Domain.Enums;
// using Portfolio.Infrastructure;
//
// namespace Portfolio.Test.Infrastructure;
//
// public class BackgroundImageQueryServiceTests
// {
//     private readonly PortfolioDbContext _dbContext;
//
//     public BackgroundImageQueryServiceTests()
//     {
//         var options = new DbContextOptionsBuilder<PortfolioDbContext>()
//             .UseInMemoryDatabase(databaseName: "TestDatabase")
//             .Options;
//         
//         _dbContext = new PortfolioDbContext(options);
//     }
//
//     [Fact]
//     public async Task Get_ReturnsImageOfTheDayDto_WhenImageExists()
//     {
//         // Arrange
//         const string url = "http://example.com/image.jpg";
//         const string altText = "Do not forget to provide a visual for the blind people too!";
//         var image = new ImageOfTheDay(url, altText, ImageOfTheDaySource.Bing, true, true);
//         _dbContext.ImageOfTheDay.Add(image);
//         await _dbContext.SaveChangesAsync();
//
//         var service = new BackgroundImageQueryService(_dbContext);
//
//         // Act
//         var result = await service.GetLatestBackgroundImageDetailsAsync();
//
//         // Assert
//         Assert.NotNull(result);
//         result.ImageUrl.Should().Be(url);
//         result.AltText.Should().Be(altText);
//         result.Source.Should().Be(ImageOfTheDaySource.Bing);
//     }
//
//     [Fact]
//     public async Task Get_ReturnsDefaultImage_WhenNoImageExists()
//     {
//         // Arrange
//         var options = new DbContextOptionsBuilder<PortfolioDbContext>()
//             .UseInMemoryDatabase(databaseName: "TestDbWithNoImage")
//             .Options;
//         var dbContext = new PortfolioDbContext(options);
//         var service = new BackgroundImageQueryService(dbContext);
//
//         // Act
//         var result = await service.GetLatestBackgroundImageDetailsAsync();
//
//         // Assert
//         Assert.NotNull(result);
//         Assert.Equal(BackgroundImageQueryService.DefaultBackgroundImage, result.ImageUrl);
//         Assert.Equal(BackgroundImageQueryService.DefaultAltText, result.ImageUrl);
//         Assert.Equal(ImageOfTheDaySource.Bing, result.Source);
//     }
//     
//     [Fact]
//     public async Task Get_ReturnsDefaultImage_WhenThereIsMultipleImages()
//     {
//         // Arrange
//         var options = new DbContextOptionsBuilder<PortfolioDbContext>()
//             .UseInMemoryDatabase(databaseName: "TestDbWithMultipleImages")
//             .Options;
//         var dbContext = new PortfolioDbContext(options);
//         var expectedRow = CreateFor_Get_ReturnsDefaultImage_WhenThereIsMultipleImages(dbContext);
//         var service = new BackgroundImageQueryService(dbContext);
//
//         // Act
//         var result = await service.GetLatestBackgroundImageDetailsAsync();
//
//         // Assert
//         Assert.NotNull(result);
//         result.ImageUrl.Should().Be(expectedRow.ImageUrl);
//         result.AltText.Should().Be(expectedRow.AltText);
//         ((byte)result.Source).Should().Be((byte)expectedRow.Source);
//     }
//
//     private ImageOfTheDay CreateFor_Get_ReturnsDefaultImage_WhenThereIsMultipleImages(PortfolioDbContext dbContext)
//     {
//         const int waitSec = 100; // 0.1 sec. Add delay to make sure the creation time is different.
//         
//         dbContext.ImageOfTheDay.AddRange(
//             new ImageOfTheDay("http://donotfindexample.com/image1.jpg", "alt1", ImageOfTheDaySource.Bing, false, true),
//             new ImageOfTheDay("http://donotfindexample.com/image2.jpg", "alt2", ImageOfTheDaySource.NASA, true, false));
//         dbContext.SaveChanges();
//         Task.Delay(waitSec).Wait();
//         
//         var valueToFind = new ImageOfTheDay("http://findexample.com/image3.jpg", "alt3", ImageOfTheDaySource.Bing, true, true);
//         dbContext.ImageOfTheDay.Add(valueToFind);
//         dbContext.SaveChanges();
//         Task.Delay(waitSec).Wait();
//         
//         dbContext.ImageOfTheDay.Add(new ImageOfTheDay("http://donotfindexample.com/image4.jpg", "alt4",  ImageOfTheDaySource.Bing, true, false));
//         dbContext.ImageOfTheDay.Add(new ImageOfTheDay("http://donotfindexample.com/image5.jpg", "alt5", ImageOfTheDaySource.Bing, false, true));
//         dbContext.SaveChanges();
//
//         return valueToFind;
//     }
// }