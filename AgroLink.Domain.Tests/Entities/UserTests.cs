using System;
using AgroLink.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace AgroLink.Domain.Tests.Entities;

[TestFixture]
public class UserTests
{
    [Test]
    public void User_CanBeCreated_WithValidData()
    {
        // Arrange
        var name = "Test User";
        var email = "test@example.com";
        var passwordHash = "hashedPassword123";
        var role = "Admin";
        var isActive = true;

        // Act
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };

        // Assert
        user.ShouldNotBeNull();
        user.Name.ShouldBe(name);
        user.Email.ShouldBe(email);
        user.PasswordHash.ShouldBe(passwordHash);
        user.Role.ShouldBe(role);
        user.IsActive.ShouldBe(isActive);
        user.CreatedAt.ShouldNotBe(default(DateTime));
    }

    [Test]
    public void User_CanUpdateName()
    {
        // Arrange
        var user = new User
        {
            Name = "Old Name",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var newName = "New Name";

        // Act
        user.Name = newName;

        // Assert
        user.Name.ShouldBe(newName);
    }
}
