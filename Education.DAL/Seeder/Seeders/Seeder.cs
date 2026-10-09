using Education.DAL.Seeder.Interfaces;

namespace Education.DAL.Seeder.Seeders
{
    public class Seeder : ISeeder
    {
        private readonly IRoleSeeder _roleSeeder;

        public Seeder(IRoleSeeder roleSeeder)
        {
            _roleSeeder = roleSeeder;
        }

        public async Task SeedAsync()
        {
            await _roleSeeder.SeedRoleAsync();
        }
    }
}
