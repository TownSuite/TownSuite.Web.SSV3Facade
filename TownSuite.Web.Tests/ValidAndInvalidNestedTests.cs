using Newtonsoft.Json;
using NUnit.Framework;
using TownSuite.Web.Example.ServiceStackExample;
using TownSuite.Web.SSV3Facade;

namespace TownSuite.Web.Tests;

[TestFixture]
public class ValidAndInvalidNestedTests
{

    [Test]
    public async Task EnsureAttributeIsCalledWith200Return()
    {
        var options = Settings.GetSettings();
        var serviceProvider = Settings.GetServiceProvider();

        string path = "https://localhost/ValidNestedExample";

        
        string value= JsonConvert.SerializeObject(new ValidNestedExample()
        {
            Input = "was this method called"
        });
   

        var facade = new ServiceStackFacade(options, serviceProvider);
        var results = await facade.Post(path, value, "any");

        Assert.That(results.json, Is.EqualTo("{\"Output\":\"All good\"}"));
        Assert.That(results.statusCode == 200);
    }

    [Test]
    public async Task EnsureAttributeIsCalledWith400Return()
    {
        var options = Settings.GetSettings();
        var serviceProvider = Settings.GetServiceProvider();

        string path = "https://localhost/ValidNestedExample";

        
        string value= JsonConvert.SerializeObject(new ValidNestedExample()
        {
            Input = "Call will fail to reach service due to executor attribute returning a 400 status"
        });
   

        var facade = new ServiceStackFacade(options, serviceProvider);
        var results = await facade.Post(path, value, "any");

        Assert.That(results.json, Is.EqualTo(null));
        Assert.That(results.statusCode == 400);
    }
    
}

