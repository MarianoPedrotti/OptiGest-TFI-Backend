using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OptiGest.Tests.Sql;
using SistemaDeVentas.Data;

[assembly: HostingStartup(typeof(OptiGest.Tests.Integrated.TestingStartup))]
namespace OptiGest.Tests.Integrated;

// Loaded only by the isolated launcher. Executes the real API Program and adds a write guard.
public sealed class TestingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        // TestServer discovers the test assembly's startup attributes too. Only the E2E launcher opts in.
        if (Environment.GetEnvironmentVariable("OPTIGEST_E2E_HOST") != "1") return;
        builder.ConfigureServices((context, services) =>
        {
            if (context.HostingEnvironment.EnvironmentName != "Testing")
                throw new InvalidOperationException("E2E startup requires Testing.");
            services.AddSingleton<DatabaseGuard>();
            services.AddTransient<IStartupFilter, GuardStartup>();
            services.AddDbContext<AppDbContext>((provider, options) => options
                .UseSqlServer(provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"))
                .AddInterceptors(provider.GetRequiredService<DatabaseGuard>()));
        });
    }

    private sealed class GuardStartup(DatabaseGuard guard, IServiceProvider services) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            using var scope = services.CreateScope();
            guard.VerifyAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>()).GetAwaiter().GetResult();
            next(app);
        };
    }
}

public sealed class DatabaseGuard(IConfiguration configuration) : SaveChangesInterceptor
{
    public async Task VerifyAsync(AppDbContext db)
    {
        var expected = configuration["TestingDatabase"] ?? "";
        TestingDatabase.ValidateDatabaseName(expected);
        var ownership = Guid.Parse(configuration["TestingOwnership"] ?? "");
        var connection = new SqlConnectionStringBuilder(db.Database.GetConnectionString());
        if (connection.DataSource != "(localdb)\\OptiGest_Testing_Phase05" || connection.InitialCatalog != expected ||
            !connection.IntegratedSecurity || connection.MultipleActiveResultSets || connection.Pooling)
            throw new InvalidOperationException("Effective E2E database configuration rejected.");
        await using var sql = new SqlConnection(connection.ConnectionString);
        await sql.OpenAsync();
        await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.OptiGestTestingIdentity WHERE Token=@token AND DB_NAME()=@name AND CAST(SERVERPROPERTY('IsLocalDB') AS int)=1", sql);
        command.Parameters.AddWithValue("@token", ownership);
        command.Parameters.AddWithValue("@name", expected);
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("E2E ownership verification failed.");
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await VerifyAsync((AppDbContext)eventData.Context!);
        return result;
    }
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        => throw new InvalidOperationException("E2E permits guarded asynchronous writes only.");
}
