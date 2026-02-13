using NSubstitute;
using PraxisNote.Application.Common;
using PraxisNote.Application.Features.AccountLinking;
using PraxisNote.Domain.Aggregates.Profiles;
using PraxisNote.Domain.Aggregates.Users;
using PraxisNote.Domain.ValueObjects;

namespace PraxisNote.Application.Tests.AccountLinking;

public class RedeemLinkCodeTests
{
    private readonly IAccountLinkCodeRepository _codeRepo = Substitute.For<IAccountLinkCodeRepository>();
    private readonly ILinkedIdentityRepository _linkedIdentityRepo = Substitute.For<ILinkedIdentityRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IProfileRepository _profileRepo = Substitute.For<IProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RedeemLinkCode _sut;

    private readonly Guid _codeOwnerUserId = Guid.NewGuid();
    private readonly Guid _redeemingUserId = Guid.NewGuid();
    private const string PlainCode = "PRAXIS-ABCD-EFGH";

    public RedeemLinkCodeTests()
    {
        _sut = new RedeemLinkCode(_codeRepo, _linkedIdentityRepo, _userRepo, _profileRepo, _unitOfWork);
    }

    #region Self-Link (Seeded by Migration)

    [Fact]
    public async Task ExecuteAsync_SeededSelfLink_SucceedsAndRemovesOldLink()
    {
        // Arrange: The redeeming user has a LinkedIdentity pointing to themselves (seeded by migration)
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", "redeemer@example.com", "Redeemer");

        var selfLink = LinkedIdentity.Create(
            userId: _redeemingUserId, // Points to self
            provider: "google",
            providerId: "redeemer-456",
            email: "redeemer@example.com",
            name: "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        SetupExistingLink(selfLink, "google", "redeemer-456");
        SetupDefaultProfile(_codeOwnerUserId);

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.MergeIntoExisting));

        // Assert: Linking should succeed
        Assert.True(result.Success);
        Assert.Equal(_codeOwnerUserId, result.TargetUserId);
        Assert.Null(result.Error);

        // The seeded self-link should have been removed
        _linkedIdentityRepo.Received(1).Remove(selfLink);

        // A new LinkedIdentity should have been added for the code owner
        await _linkedIdentityRepo.Received(1).AddAsync(
            Arg.Is<LinkedIdentity>(li => li.UserId == _codeOwnerUserId),
            Arg.Any<CancellationToken>());

        // The redeeming user should have been removed
        _userRepo.Received(1).Remove(redeemingUser);

        // Changes should have been persisted
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Already Linked to Target

    [Fact]
    public async Task ExecuteAsync_AlreadyLinkedToCodeOwner_ReturnsFriendlyError()
    {
        // Arrange: The redeeming user's identity is already linked to the code owner
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", "redeemer@example.com", "Redeemer");

        var existingLink = LinkedIdentity.Create(
            userId: _codeOwnerUserId, // Points to code owner already
            provider: "google",
            providerId: "redeemer-456",
            email: "redeemer@example.com",
            name: "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        SetupExistingLink(existingLink, "google", "redeemer-456");

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.MergeIntoExisting));

        // Assert: Should return friendly error, not the generic "already linked" error
        Assert.False(result.Success);
        Assert.Equal(_codeOwnerUserId, result.TargetUserId);
        Assert.Equal(RedeemLinkCode.AlreadyLinkedToTargetError, result.Error);
    }

    #endregion

    #region Linked to Third-Party Account

    [Fact]
    public async Task ExecuteAsync_LinkedToThirdPartyAccount_ReturnsAlreadyLinkedError()
    {
        // Arrange: The redeeming user's identity is linked to a different user (not self, not code owner)
        var thirdPartyUserId = Guid.NewGuid();
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", "redeemer@example.com", "Redeemer");

        var thirdPartyLink = LinkedIdentity.Create(
            userId: thirdPartyUserId, // Points to a third-party account
            provider: "google",
            providerId: "redeemer-456",
            email: "redeemer@example.com",
            name: "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        SetupExistingLink(thirdPartyLink, "google", "redeemer-456");

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.MergeIntoExisting));

        // Assert: Should block with the generic "already linked" error
        Assert.False(result.Success);
        Assert.Equal(_redeemingUserId, result.TargetUserId);
        Assert.Equal(RedeemLinkCode.AlreadyLinkedError, result.Error);
    }

    #endregion

    #region No Existing Link (First-Time, No Migration Seed)

    [Fact]
    public async Task ExecuteAsync_NoExistingLink_Succeeds()
    {
        // Arrange: No LinkedIdentity exists for the redeeming user at all
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", "redeemer@example.com", "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        _linkedIdentityRepo.GetByProviderAsync("google", "redeemer-456", Arg.Any<CancellationToken>())
            .Returns((LinkedIdentity?)null);
        SetupDefaultProfile(_codeOwnerUserId);

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.MergeIntoExisting));

        // Assert: Should succeed without removing any existing link
        Assert.True(result.Success);
        Assert.Equal(_codeOwnerUserId, result.TargetUserId);

        // No Remove call should have been made
        _linkedIdentityRepo.DidNotReceive().Remove(Arg.Any<LinkedIdentity>());

        // A new LinkedIdentity should have been added
        await _linkedIdentityRepo.Received(1).AddAsync(
            Arg.Is<LinkedIdentity>(li => li.UserId == _codeOwnerUserId),
            Arg.Any<CancellationToken>());

        // Changes should have been persisted
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create New Profile

    [Fact]
    public async Task ExecuteAsync_CreateNewProfile_UsesEmailAsProfileName()
    {
        // Arrange
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", "redeemer@example.com", "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        _linkedIdentityRepo.GetByProviderAsync("google", "redeemer-456", Arg.Any<CancellationToken>())
            .Returns((LinkedIdentity?)null);
        _profileRepo.GetCountByUserIdAsync(_codeOwnerUserId, Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.CreateNewProfile));

        // Assert
        Assert.True(result.Success);
        await _profileRepo.Received(1).AddAsync(
            Arg.Is<Profile>(p => p.Name == "redeemer@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CreateNewProfile_TruncatesLongEmailAsProfileName()
    {
        // Arrange
        var codeOwner = CreateUser(_codeOwnerUserId, "google", "owner-123", "owner@example.com", "Owner");
        var longEmail = "redeemer." + new string('x', 160) + "@example.com"; // > 100 characters
        var redeemingUser = CreateUser(_redeemingUserId, "google", "redeemer-456", longEmail, "Redeemer");

        SetupValidCode(_codeOwnerUserId);
        SetupUsers(codeOwner, redeemingUser);
        _linkedIdentityRepo.GetByProviderAsync("google", "redeemer-456", Arg.Any<CancellationToken>())
            .Returns((LinkedIdentity?)null);
        _profileRepo.GetCountByUserIdAsync(_codeOwnerUserId, Arg.Any<CancellationToken>()).Returns(0);

        // Act
        var result = await _sut.ExecuteAsync(new RedeemLinkCode.Command(
            _redeemingUserId, PlainCode, MergeStrategy.CreateNewProfile));

        // Assert
        Assert.True(result.Success);
        await _profileRepo.Received(1).AddAsync(
            Arg.Is<Profile>(p => !string.IsNullOrEmpty(p.Name) && p.Name.Length <= 100 && p.Name.EndsWith("...")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static User CreateUser(Guid userId, string provider, string providerId, string email, string name)
    {
        var user = User.Register(
            new ExternalIdentity(provider, providerId),
            new Email(email),
            name);

        // Use reflection to set the ID since it's auto-generated in the factory method
        var idProp = typeof(User).GetProperty("Id")!;
        idProp.SetValue(user, userId);

        return user;
    }

    private void SetupValidCode(Guid ownerUserId)
    {
        var codeHash = LinkCodeService.HashCode(PlainCode);
        var code = AccountLinkCode.Create(ownerUserId, codeHash, TimeSpan.FromMinutes(10));
        _codeRepo.GetByHashAsync(codeHash, Arg.Any<CancellationToken>()).Returns(code);
    }

    private void SetupUsers(User codeOwner, User redeemingUser)
    {
        _userRepo.GetByIdAsync(codeOwner.Id, Arg.Any<CancellationToken>()).Returns(codeOwner);
        _userRepo.GetByIdAsync(redeemingUser.Id, Arg.Any<CancellationToken>()).Returns(redeemingUser);
    }

    private void SetupExistingLink(LinkedIdentity link, string provider, string providerId)
    {
        _linkedIdentityRepo.GetByProviderAsync(
            provider, providerId, Arg.Any<CancellationToken>())
            .Returns(link);
    }

    private void SetupDefaultProfile(Guid userId)
    {
        var profile = Profile.Create(userId, "Default");
        _profileRepo.GetDefaultByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
    }

    #endregion
}
