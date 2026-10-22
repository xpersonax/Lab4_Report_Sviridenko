using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Data;

public interface ITrainRouteRepository
{
    Task<List<TrainRoute>> GetAllAsync();
    Task<TrainRoute?> GetByIdAsync(int id);
    Task<int> SaveAsync(TrainRoute route);
    Task<int> DeleteAsync(TrainRoute route);
}
