using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaDeVentas.Data;
using SistemaDeVentas.Repositories;
using SistemaDeVentas.Services;

namespace OptiGest.Tests.Sql;

// No host ASP.NET, UserSecrets, environment connection strings or default config.
public sealed record TestingSettings(string Environment, string LocalDbInstance, string DatabasePrefix)
{
    public void Validate()
    {
        if (Environment != "Testing" || LocalDbInstance != "OptiGest_Testing_Phase05" ||
            DatabasePrefix != "OptiGest_Testing_Phase05_")
            throw new InvalidOperationException("Only the dedicated Phase05 Testing target is permitted.");
    }
}

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OPTIGEST_RUN_SQL_TESTS") != "1")
            Skip = "Opt-in required: OPTIGEST_RUN_SQL_TESTS=1; dedicated LocalDB only.";
    }
}

public sealed class SqlTheoryAttribute : TheoryAttribute
{
    public SqlTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("OPTIGEST_RUN_SQL_TESTS") != "1")
            Skip = "Opt-in required: OPTIGEST_RUN_SQL_TESTS=1; dedicated LocalDB only.";
    }
}

[CollectionDefinition("TestingSql", DisableParallelization = true)]
public sealed class TestingSqlCollection : ICollectionFixture<TestingDatabase>;

public sealed class TestingDatabase : IAsyncLifetime
{
    private string _connectionString = "";
    private string _identity = "";
    public string DatabaseName { get; private set; } = "";
    public Guid Ownership { get; } = Guid.NewGuid();
    public bool Enabled => Environment.GetEnvironmentVariable("OPTIGEST_RUN_SQL_TESTS") == "1";

    public async Task InitializeAsync()
    {
        if (!Enabled) return;
        var settings = JsonSerializer.Deserialize<TestingSettings>(
            await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json")))
            ?? throw new InvalidOperationException("Missing explicit Testing config.");
        settings.Validate();

        // Verify this named instance exists; never auto-create or use MSSQLLocalDB/SQLEXPRESS.
        using var process = Process.Start(new ProcessStartInfo("SqlLocalDB.exe", $"info {settings.LocalDbInstance}")
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false })
            ?? throw new InvalidOperationException("LocalDB cannot be verified.");
        var info = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        if (process.ExitCode != 0 || !info.Contains(settings.LocalDbInstance, StringComparison.Ordinal))
            throw new InvalidOperationException("Provision the dedicated instance explicitly before SQL tests.");
        var pipeMatch = Regex.Match(info, @"LOCALDB#[A-Z0-9]+", RegexOptions.IgnoreCase);
        if (!pipeMatch.Success) throw new InvalidOperationException("Dedicated instance is not running.");

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"(localdb)\\{settings.LocalDbInstance}", InitialCatalog = "master",
            IntegratedSecurity = true, Encrypt = false, Pooling = false, ConnectTimeout = 10,
            MultipleActiveResultSets = false, ApplicationName = "OptiGest.Testing.Phase05"
        };
        await using var master = new SqlConnection(builder.ConnectionString);
        await master.OpenAsync();
        _identity = await VerifyServerAsync(master);
        if (!string.Equals(_identity, pipeMatch.Value, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Connection resolved to a different LocalDB process.");

        // Refuse this instance if someone has subsequently placed foreign databases in it.
        await using (var inventory = new SqlCommand("SELECT name FROM sys.databases WHERE database_id > 4", master))
        await using (var names = await inventory.ExecuteReaderAsync())
        {
            while (await names.ReadAsync()) ValidateDatabaseName(names.GetString(0));
        }

        DatabaseName = settings.DatabasePrefix + Guid.NewGuid().ToString("N");
        ValidateDatabaseName(DatabaseName);
        await using (var check = new SqlCommand("SELECT DB_ID(@name)", master))
        {
            check.Parameters.AddWithValue("@name", DatabaseName);
            if (await check.ExecuteScalarAsync() is not DBNull)
                throw new InvalidOperationException("Refusing to adopt an existing database.");
        }
        // All identifiers below are generated internally and validated; never user input.
        await using (var create = new SqlCommand($"CREATE DATABASE [{DatabaseName}]", master))
            await create.ExecuteNonQueryAsync();

        builder.InitialCatalog = DatabaseName;
        _connectionString = builder.ConnectionString;
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await VerifyServerAsync(connection);
        await using (var marker = new SqlCommand(
            "CREATE TABLE dbo.OptiGestTestingIdentity (Token uniqueidentifier NOT NULL PRIMARY KEY); " +
            "INSERT dbo.OptiGestTestingIdentity(Token) VALUES (@token);", connection))
        {
            marker.Parameters.AddWithValue("@token", Ownership);
            await marker.ExecuteNonQueryAsync();
        }
        await VerifyOwnershipAsync();
        await using var context = await CreateContextAsync();
        // Existing migrations only, on the NEW owned Testing database.
        await context.Database.MigrateAsync();
        var results = Path.Combine(AppContext.BaseDirectory, "TestResults");
        Directory.CreateDirectory(results);
        await File.WriteAllTextAsync(Path.Combine(results, "testing-database.json"), JsonSerializer.Serialize(new
        { Instance = settings.LocalDbInstance, EffectiveInstance = _identity, DatabaseName, Ownership }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[Testing SQL] {settings.LocalDbInstance} / {DatabaseName} (synthetic; retained)");
    }

    public static void ValidateDatabaseName(string name)
    {
        if (!Regex.IsMatch(name, @"\AOptiGest_Testing_Phase05_[a-f0-9]{32}\z"))
            throw new InvalidOperationException("Non-Testing database rejected.");
    }

    private static async Task<string> VerifyServerAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand(
            "SELECT CAST(SERVERPROPERTY('IsLocalDB') AS int), CAST(SERVERPROPERTY('InstanceName') AS nvarchar(128))", connection);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync() || reader.IsDBNull(0) || reader.GetInt32(0) != 1)
            throw new InvalidOperationException("SQL target is not LocalDB.");
        return reader.GetString(1);
    }

    public async Task VerifyOwnershipAsync()
    {
        if (!Enabled || string.IsNullOrEmpty(_connectionString)) throw new InvalidOperationException("SQL tests are not enabled.");
        ValidateDatabaseName(DatabaseName);
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        if (!string.Equals(await VerifyServerAsync(connection), _identity, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Testing instance changed: stop and re-provision explicitly.");
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.OptiGestTestingIdentity WHERE Token=@token AND DB_NAME()=@name", connection);
        command.Parameters.AddWithValue("@token", Ownership);
        command.Parameters.AddWithValue("@name", DatabaseName);
        if (Convert.ToInt32(await command.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("Database ownership marker does not match this test run.");
    }

    public async Task<AppDbContext> CreateContextAsync(params IInterceptor[] interceptors)
    {
        await VerifyOwnershipAsync();
        return BuildContext(interceptors);
    }

    public AppDbContext CreateContextForHost()
    {
        if (!Enabled || string.IsNullOrEmpty(_connectionString)) throw new InvalidOperationException("Testing SQL is not initialized.");
        ValidateDatabaseName(DatabaseName);
        // Host is built only after VerifyOwnershipAsync; every SaveChanges remains guarded.
        return BuildContext([]);
    }

    private AppDbContext BuildContext(IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString)
            .AddInterceptors(new OwnershipInterceptor(this)).AddInterceptors(interceptors).Options;
        return new AppDbContext(options);
    }

    public static VentaService Sales(AppDbContext context)
    {
        var operation = new OperacionFinancieraRepository(context);
        return new(new VentaRepository(context), new ClienteRepository(context), new ProductoRepository(context),
            new CuentaCorrienteRepository(context, operation), new VehiculoRepository(context), operation, new UsuarioRepository(context));
    }
    public static CuentaCorrienteRepository Ledger(AppDbContext context) => new(context, new OperacionFinancieraRepository(context));
    public static FinanciacionService Financing(AppDbContext context)
    {
        var operation = new OperacionFinancieraRepository(context);
        return new(new FinanciacionRepository(context), new VentaRepository(context), new ConfiguracionFinanciacionRepository(context),
            new CuotaRepository(context), new CuentaCorrienteRepository(context, operation), operation);
    }
    public static PagoVentaService Payments(AppDbContext context)
    {
        var operation = new OperacionFinancieraRepository(context);
        return new(new PagoVentaRepository(context), new VentaRepository(context), new CuentaCorrienteRepository(context, operation), operation);
    }
    public static PagoCuotaService InstallmentPayments(AppDbContext context)
    {
        var operation = new OperacionFinancieraRepository(context);
        return new(new PagoCuotaRepository(context), new CuotaRepository(context), new FinanciacionRepository(context),
            new CuentaCorrienteRepository(context, operation), new VentaRepository(context), operation);
    }

    public Task DisposeAsync() => Task.CompletedTask; // Never drop databases automatically.

    private sealed class OwnershipInterceptor(TestingDatabase database) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await database.VerifyOwnershipAsync();
            return result;
        }
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
            => throw new InvalidOperationException("Tests permit only guarded asynchronous writes.");
    }
}
