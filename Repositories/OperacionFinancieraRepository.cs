using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SistemaDeVentas.Data;
using SistemaDeVentas.Helpers;
using SistemaDeVentas.Repositories.Interfaces;

namespace SistemaDeVentas.Repositories;

// Unit of transaction only: business rules remain in the existing services.
public sealed class OperacionFinancieraRepository(AppDbContext context) : IOperacionFinancieraRepository
{
    private Guid? _transactionId;
    private int? _clienteBloqueado;

    public async Task<T> EjecutarAsync<T>(int clienteId, Func<Task<T>> operacion)
    {
        if (clienteId <= 0) throw new ReglaFinancieraException("Cliente inválido.", "cliente_invalido", 400);
        if (context.Database.CurrentTransaction != null)
            throw new InvalidOperationException("La operación financiera debe controlar su propia transacción.");
        if (context.Database.CreateExecutionStrategy().RetriesOnFailure)
            throw new InvalidOperationException("No se habilita replay automático de escrituras financieras.");
        if (context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("La operación financiera no admite cambios previos sin guardar.");

        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            await BloquearClienteAsync(clienteId);
            // Queries after acquiring the lock must not reuse older tracked snapshots.
            context.ChangeTracker.Clear();
            var result = await operacion();
            try { await transaction.CommitAsync(); }
            catch (Exception error) { throw new ResultadoFinancieroDesconocidoException(error); }
            return result;
        }
        catch (ResultadoFinancieroDesconocidoException)
        {
            context.ChangeTracker.Clear();
            throw; // Do not replay or assert that the database rolled back an uncertain commit.
        }
        catch (Exception error)
        {
            var sql = error as SqlException ?? error.InnerException as SqlException;
            // SQL Server already rolls back a deadlock victim (1205). A second
            // rollback can itself fail because that transaction is completed.
            try { if (sql?.Number != 1205) await transaction.RollbackAsync(); }
            catch (Exception rollbackError) { throw new ResultadoFinancieroDesconocidoException(new AggregateException(error, rollbackError)); }
            finally { context.ChangeTracker.Clear(); }
            if (sql?.Number is 1205 or 1222)
                throw new ReglaFinancieraException("La operación entró en conflicto con otra operación. Actualice su estado.", "conflicto_concurrencia", 409, error);
            throw;
        }
        finally { _transactionId = null; _clienteBloqueado = null; }
    }

    public async Task BloquearClienteAsync(int clienteId)
    {
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("El bloqueo financiero requiere una transacción.");
        if (_transactionId == transaction.TransactionId)
        {
            if (_clienteBloqueado != clienteId)
                throw new InvalidOperationException("Una operación financiera solo puede afectar a un cliente.");
            return;
        }
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandTimeout = 15;
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock " +
            "@Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; SELECT @result;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.DbType = DbType.String;
        parameter.Value = $"OptiGest:Cliente:{clienteId}";
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result is -1 or -2 or -3)
            throw new ReglaFinancieraException("El cliente tiene otra operación en curso. Actualice su estado.", "cliente_ocupado");
        if (result < 0) throw new InvalidOperationException("No se pudo adquirir el bloqueo financiero.");
        _transactionId = transaction.TransactionId;
        _clienteBloqueado = clienteId;
    }
}
