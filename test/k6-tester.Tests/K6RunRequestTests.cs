using K6Tester.Models;

namespace K6Tester.Tests;

public class K6RunRequestTests
{
    [Fact]
    public void Properties_CanBeAssigned()
    {
        var request = new K6RunRequest
        {
            Script = "console.log('hi');",
            FileName = "test.js"
        };

        Assert.Equal("console.log('hi');", request.Script);
        Assert.Equal("test.js", request.FileName);
    }
}
