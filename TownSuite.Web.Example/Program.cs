using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using Prometheus;
using SimpleInjector;
using TownSuite.Web.Example.ServiceStackExample;
using TownSuite.Web.SSV3Facade;
using TownSuite.Web.SSV3Facade.Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
//builder.Services.AddScoped<IDoStuff>((s) =>
//{
//    return new DoStuff()
//    {
//        Test = DateTime.UtcNow.ToString()
//    };
//});

var simpleInjectorContainer = SimpleInjectorTesting();
object p = builder.Services.AddSimpleInjector(simpleInjectorContainer, options =>
{
    // AddAspNetCore() wraps web requests in a Simple Injector scope and
    // allows request-scoped framework services to be resolved.
    options.AddAspNetCore()

        // Ensure activation of a specific framework type to be created by
        // Simple Injector instead of the built-in configuration system.
        // All calls are optional. You can enable what you need. For instance,
        // ViewComponents, PageModels, and TagHelpers are not needed when you
        // build a Web API.
        .AddControllerActivation();

    // Optionally, allow application components to depend on the non-generic
    // ILogger (Microsoft.Extensions.Logging) or IStringLocalizer
    // (Microsoft.Extensions.Localization) abstractions.
    options.AddLogging();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// REQUIRED
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swag/swagger.json", "v1");
    });
}

app.UseHttpsRedirection();


//REQUIRED
app.UseRouting();

app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    // ...

    endpoints.MapMetrics();
});

// If a service has an attribute that implements TownSuite.Web.SSV3Facade.IExecutableAttribute
// its ExecuteAsync method will invoked.  This is to help port over
// old code.  Create an attribute that also implmentats IExecutableAttribute.
// If this is not wanted, or some other action is desired then
// implment the CustomCallBack delegate and watch for
// callbackType == CustomCall.ServiceInstantiated.  This method
// will be called after the service is instantiated but before any methods
// of the service are invoked.
//

TownSuite.Web.SSV3Facade.Prometheus.SsPrometheus.Initialize("");

// Add sql observer
var observer = new SqlClientObserver("townsuite_");
IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(observer);

// REQUIRED
app.UseServiceStackV3Facade(new ServiceStackV3FacadeOptions(
    serviceTypes: new Type[] {
        typeof(TownSuite.Web.Example.ServiceStackExample.BaseServiceExample)
    })
{
    RoutePath = "/example/service/json/syncreply/{name}",
    SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
    {
        NullValueHandling = Newtonsoft.Json.NullValueHandling.Include,
        ContractResolver = new AllPropertiesResolver(),
        Converters = {
                        new BackwardsCompatStringConverter()
                    }
    },
    SearchAssemblies = new Assembly[]
     {
         Assembly.Load("TownSuite.Web.Example")
     },
    CustomCallBack = ((CustomCall callbackType, object serviceInstance,
          object? requestDto) args) =>
    {
        // Example to demonstrate custom callback hook types

        if (args.callbackType == CustomCall.ServiceInstantiated)
        {
            Console.WriteLine($"Service {args.serviceInstance.GetType().ToString()} initialized.");
            // This is the place to do custom authorization work or
            // other custom work that needs to take place before service methods are called
        }


        // Example of executing an extra method regardless based solely on serviceInstance type
        if (args.serviceInstance.GetType() == typeof(TownSuite.Web.Example.ServiceStackExample.ExampleService))
        {
            var serviceInstance = (args.serviceInstance as TownSuite.Web.Example.ServiceStackExample.ExampleService);
            serviceInstance?.SomeOtherExampleMethod();
        }

        return Task.CompletedTask;
    },
    CustomErrorHandler = async (
        (Type Service, MethodInfo Method, Type DtoType)? serviceInfo,
        object instance, Exception ex
        ) =>
    {
        // Example to demonstrate a custom error handling hook

        return await Task.FromResult<(string Output, bool ReThrow)>(
            (Output: "Demonstratate a custom result can be returned.",
            ReThrow: true));
    },
    Prometheus = () =>
    {
        return new TownSuite.Web.SSV3Facade.Prometheus.SsPrometheus();
    }

}, serviceProvider: simpleInjectorContainer
    );

app.UseServiceStackV3FacadeSwagger(new ServiceStackV3FacadeOptions(
    serviceTypes: new Type[] {
        typeof(TownSuite.Web.Example.ServiceStackExample.BaseServiceExample)
    })
{
    SwaggerPath = "/swag/swagger.json",
    RoutePath = "/example/service/json/syncreply",
    SearchAssemblies = new Assembly[]
     {
         Assembly.Load("TownSuite.Web.Example")
     },
});



app.MapControllers();


app.Run();


Container SimpleInjectorTesting()
{

    var container = new Container();
    container.Options.DefaultScopedLifestyle = new SimpleInjector.Lifestyles.AsyncScopedLifestyle();
    container.Options.EnableAutoVerification = false;

    container.RegisterSingleton<IHttpContextAccessor, HttpContextAccessor>();
    container.Register<IDoStuff>(() =>
    {
        return new DoStuff()
        {
            Test = DateTime.UtcNow.ToString()
        };
    });
    return container;
}
