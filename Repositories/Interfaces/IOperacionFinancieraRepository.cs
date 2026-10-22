namespace SistemaDeVentas.Repositories.Interfaces;

public interface IOperacionFinancieraRepository
{
    Task<T> EjecutarAsync<T>(int clienteId, Func<Task<T>> operacion);
    Task BloquearClienteAsync(int clienteId);
}
