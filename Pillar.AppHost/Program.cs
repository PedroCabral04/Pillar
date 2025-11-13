var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL Database
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("erp");

// Pillar ERP Application  
var pillarErp = builder.AddProject<Projects.erp>("pillar-erp")
    .WithReference(postgres)
    .WithHttpsEndpoint(port: 8081, name: "https")
    .WithExternalHttpEndpoints();

builder.Build().Run();
