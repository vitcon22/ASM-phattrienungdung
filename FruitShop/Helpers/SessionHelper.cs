using System.Text.Json;

namespace FruitShop.Helpers
{
    /// <summary>
    /// Helper xử lý Session - lưu/lấy object từ Session
    /// </summary>
    public static class SessionHelper
    {
        // Keys cho Session
        public const string UserIdKey    = "UserId";
        public const string UserNameKey  = "UserName";
        public const string UserRoleKey  = "UserRole";
        public const string CartKey      = "Cart";

        /// <summary>
        /// Lưu object vào session dạng JSON
        /// </summary>
        public static void SetObject<T>(ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        /// <summary>
        /// Lấy object từ session JSON
        /// </summary>
        public static T? GetObject<T>(ISession session, string key)
        {
            var json = session.GetString(key);
            return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Lấy UserId từ session
        /// </summary>
        public static int GetUserId(ISession session)
        {
            return session.GetInt32(UserIdKey) ?? 0;
        }

        /// <summary>
        /// Lấy Role của user hiện tại
        /// </summary>
        public static string GetUserRole(ISession session)
        {
            return session.GetString(UserRoleKey) ?? string.Empty;
        }

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa
        /// </summary>
        public static bool IsLoggedIn(ISession session)
        {
            return session.GetInt32(UserIdKey).HasValue;
        }

        /// <summary>
        /// Kiểm tra user có role cụ thể không
        /// </summary>
        public static bool HasRole(ISession session, string role)
        {
            return GetUserRole(session).Equals(role, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Kiểm tra user có một trong các role được chỉ định không
        /// </summary>
        public static bool HasAnyRole(ISession session, params string[] roles)
        {
            var currentRole = GetUserRole(session);
            return roles.Any(r => r.Equals(currentRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Dọn session khi đăng xuất
        /// </summary>
        public static void Clear(ISession session)
        {
            session.Clear();
        }
    }
}
