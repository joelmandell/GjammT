using Aspire.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<GjammT>("BlazorMainApp").WithUrl("https://demo.gjammt.se:7249");
builder.Build().Run();