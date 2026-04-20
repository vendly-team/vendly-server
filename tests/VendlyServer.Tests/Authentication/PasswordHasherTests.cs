using VendlyServer.Infrastructure.Authentication;

namespace VendlyServer.Tests.Authentication;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hash = _hasher.Hash("MyPassword123");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Hash_FormatIs_Salt_Dot_Key()
    {
        var parts = _hasher.Hash("MyPassword123").Split('.');

        Assert.Equal(2, parts.Length);
        Assert.False(string.IsNullOrWhiteSpace(parts[0]));
        Assert.False(string.IsNullOrWhiteSpace(parts[1]));
    }

    [Fact]
    public void Hash_SamePassword_ProducesUniqueSalts()
    {
        var hash1 = _hasher.Hash("SamePassword");
        var hash2 = _hasher.Hash("SamePassword");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("CorrectPassword123");

        Assert.True(_hasher.Verify("CorrectPassword123", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _hasher.Hash("CorrectPassword");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForEmptyHash()
    {
        Assert.False(_hasher.Verify("SomePassword", string.Empty));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForMalformedHash_NoDotSeparator()
    {
        Assert.False(_hasher.Verify("SomePassword", "invalidhashwithoutdot"));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForTamperedKeyPart()
    {
        var hash = _hasher.Hash("OriginalPassword");
        var salt = hash.Split('.')[0];
        var tampered = $"{salt}.{Convert.ToBase64String(new byte[32])}";

        Assert.False(_hasher.Verify("OriginalPassword", tampered));
    }

    [Fact]
    public void Hash_HandlesEmptyStringPassword()
    {
        var hash = _hasher.Hash(string.Empty);

        Assert.Equal(2, hash.Split('.').Length);
        Assert.True(_hasher.Verify(string.Empty, hash));
    }

    [Fact]
    public void Hash_HandlesUnicodeAndSpecialCharacters()
    {
        const string password = "密碼123!@#$%^&*()АБВ";
        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }
}
