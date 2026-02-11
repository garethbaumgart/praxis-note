using PraxisNote.Domain.Aggregates.Profiles;

namespace PraxisNote.Domain.Tests.Aggregates;

public class ProfileTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    #region Create

    [Fact]
    public void Create_WithValidInputs_ReturnsProfileWithCorrectProperties()
    {
        // Arrange
        var name = "Work";

        // Act
        var profile = Profile.Create(_validUserId, name);

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(_validUserId, profile.UserId);
        Assert.Equal("Work", profile.Name);
        Assert.Null(profile.Icon);
        Assert.False(profile.IsDefault);
        Assert.True(profile.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(profile.CreatedAt, profile.UpdatedAt);
    }

    [Fact]
    public void Create_WithIcon_SetsIcon()
    {
        // Arrange & Act
        var profile = Profile.Create(_validUserId, "Work", icon: "briefcase");

        // Assert
        Assert.Equal("briefcase", profile.Icon);
    }

    [Fact]
    public void Create_WithIsDefaultTrue_SetsIsDefault()
    {
        // Arrange & Act
        var profile = Profile.Create(_validUserId, "Main", isDefault: true);

        // Assert
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Profile.Create(Guid.Empty, "Work"));
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => Profile.Create(_validUserId, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => Profile.Create(_validUserId, invalidName));
    }

    [Fact]
    public void Create_WithNameWithSpaces_TrimsName()
    {
        // Arrange & Act
        var profile = Profile.Create(_validUserId, "  Work  ");

        // Assert
        Assert.Equal("Work", profile.Name);
    }

    #endregion

    #region CreateDefault

    [Fact]
    public void CreateDefault_ReturnsProfileWithDefaultProperties()
    {
        // Arrange & Act
        var profile = Profile.CreateDefault(_validUserId);

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(_validUserId, profile.UserId);
        Assert.Equal("Default", profile.Name);
        Assert.Null(profile.Icon);
        Assert.True(profile.IsDefault);
    }

    [Fact]
    public void CreateDefault_WithEmptyUserId_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Profile.CreateDefault(Guid.Empty));
    }

    #endregion

    #region Rename

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Old Name");
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.Rename("New Name");

        // Assert
        Assert.Equal("New Name", profile.Name);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Rename_WithNameWithSpaces_TrimsName()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Old Name");

        // Act
        profile.Rename("  New Name  ");

        // Assert
        Assert.Equal("New Name", profile.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => profile.Rename(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithEmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Valid Name");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => profile.Rename(invalidName));
    }

    #endregion

    #region SetIcon

    [Fact]
    public void SetIcon_WithValue_UpdatesIcon()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Work");
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.SetIcon("star");

        // Assert
        Assert.Equal("star", profile.Icon);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void SetIcon_WithNull_ClearsIcon()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Work", icon: "star");

        // Act
        profile.SetIcon(null);

        // Assert
        Assert.Null(profile.Icon);
    }

    #endregion

    #region SetAsDefault / ClearDefault

    [Fact]
    public void SetAsDefault_SetsIsDefaultToTrue()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Work");
        Assert.False(profile.IsDefault);
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.SetAsDefault();

        // Assert
        Assert.True(profile.IsDefault);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void ClearDefault_SetsIsDefaultToFalse()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Work", isDefault: true);
        Assert.True(profile.IsDefault);
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.ClearDefault();

        // Assert
        Assert.False(profile.IsDefault);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void SetAsDefault_WhenAlreadyDefault_StillUpdatesTimestamp()
    {
        // Arrange
        var profile = Profile.Create(_validUserId, "Work", isDefault: true);
        var originalUpdatedAt = profile.UpdatedAt;

        // Act
        profile.SetAsDefault();

        // Assert
        Assert.True(profile.IsDefault);
        Assert.True(profile.UpdatedAt >= originalUpdatedAt);
    }

    #endregion
}
