using TodoList.API.Helpers;

namespace TodoList.Test;

public class PasswordHelperTests
{
    [Fact]
    public void CreateAndVerify_ReturnsTrue_ForSamePassword()
    {
        PasswordHelper.CreatePasswordHash("Secret123!", out var hash, out var salt);

        var result = PasswordHelper.VerifyPasswordHash("Secret123!", hash, salt);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_ForDifferentPassword()
    {
        PasswordHelper.CreatePasswordHash("Secret123!", out var hash, out var salt);

        var result = PasswordHelper.VerifyPasswordHash("WrongPassword!", hash, salt);

        Assert.False(result);
    }
}
