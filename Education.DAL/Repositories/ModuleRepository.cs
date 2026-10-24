using Education.DAL.Context;
using Education.DAL.Interfaces;
using Education.Domain.Entity;

namespace Education.DAL.Repositories
{
    public class ModuleRepository : BaseRepository<ModuleEntity>, IModuleRepository
    {
        public ModuleRepository(EducationDbContext context) : base(context) { }
    }
}