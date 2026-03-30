using Microsoft.Data.SqlClient;
using System.Data;

namespace FruitShop.Models.DAL
{
    /// <summary>
    /// Cung cấp kết nối database sử dụng Dapper
    /// </summary>
    public class FruitShopContext
    {
        private readonly string _connectionString;

        public FruitShopContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("FruitShopDB")
                ?? throw new InvalidOperationException("Không tìm thấy connection string 'FruitShopDB'");
        }

        /// <summary>
        /// Tạo và trả về một IDbConnection mới
        /// </summary>
        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
