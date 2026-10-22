using SistemaDeVentas.Entities;

namespace SistemaDeVentas.Repositories.Interfaces
{
    public interface ISubCategoriaRepository
    {
        Task<List<SubCategoria>> ObtenerTodosAsync();

        Task<SubCategoria?> ObtenerPorIdAsync(int id);

        Task<SubCategoria?> ObtenerPorNombreAsync(string nombre);

        Task<SubCategoria> CrearAsync(SubCategoria subCategoria);

        Task ActualizarAsync(SubCategoria subCategoria);

        Task EliminarAsync(SubCategoria subCategoria);
        Task<List<SubCategoria>> ObtenerPorCategoriaAsync(int categoriaId);
    }
}