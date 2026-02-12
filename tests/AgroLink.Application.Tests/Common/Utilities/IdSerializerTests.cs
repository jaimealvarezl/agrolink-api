using AgroLink.Application.Common.Utilities;
using Shouldly;

namespace AgroLink.Application.Tests.Common.Utilities;

[TestFixture]
public class IdSerializerTests
{
    [TestCase("Farm", 1)]
    [TestCase("Animal", 42)]
    [TestCase("AnimalPhoto", 1005)]
    [TestCase("VeryLongTypeName", 999999)]
    public void Encode_ThenDecode_ReturnsOriginalValues(string type, int id)
    {
        // Act
        var encoded = IdSerializer.Encode(type, id);
        var (decodedType, decodedId) = IdSerializer.Decode(encoded);

        // Assert
        encoded.ShouldNotBeNullOrWhiteSpace();
        decodedType.ShouldBe(type);
        decodedId.ShouldBe(id);
    }

    [Test]
    public void Encode_ReturnsBase64UrlSafeString()
    {
        // Arrange
        var type = "Test";
        var id = 12345;

        // Act
        var encoded = IdSerializer.Encode(type, id);

        // Assert
        encoded.ShouldNotContain("+");
        encoded.ShouldNotContain("/");
        encoded.ShouldNotContain("=");
    }

    [Test]
    public void Decode_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        // Base64Url for "InvalidID" (no colon)
        var invalid = "SW52YWxpZElE";

        // Act & Assert
        Should
            .Throw<ArgumentException>(() => IdSerializer.Decode(invalid))
            .Message.ShouldBe("Invalid Global ID format");
    }
}
