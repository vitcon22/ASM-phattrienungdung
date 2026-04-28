using BCrypt.Net;

Console.WriteLine("BCrypt hashes for FruitShop test accounts:");
Console.WriteLine($"Admin@123     => {BCrypt.Net.BCrypt.HashPassword("Admin@123")}");
Console.WriteLine($"Staff@123     => {BCrypt.Net.BCrypt.HashPassword("Staff@123")}");
Console.WriteLine($"Customer@123  => {BCrypt.Net.BCrypt.HashPassword("Customer@123")}");
Console.WriteLine($"NewUser@123   => {BCrypt.Net.BCrypt.HashPassword("NewUser@123")}");
