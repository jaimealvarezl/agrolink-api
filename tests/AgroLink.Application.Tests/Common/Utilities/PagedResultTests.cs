using AgroLink.Application.Common.Utilities;
using Shouldly;

namespace AgroLink.Application.Tests.Common.Utilities;

[TestFixture]
public class PagedResultTests
{
    [Test]
    public void TotalPages_WhenPageSizeIsZero_ReturnsZero()
    {
        // Arrange
        var items = new List<string>();
        var totalCount = 10;
        var page = 1;
        var pageSize = 0;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        result.TotalPages.ShouldBe(0);
    }

    [Test]
    public void TotalPages_WhenPageSizeIsPositive_ReturnsCorrectCount()
    {
        // Arrange
        var items = new List<string>();
        var totalCount = 10;
        var page = 1;
        var pageSize = 3;

        // Act
        var result = new PagedResult<string>(items, totalCount, page, pageSize);

        // Assert
        result.TotalPages.ShouldBe(4); // 10 / 3 = 3.33 -> 4
    }
}
