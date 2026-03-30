namespace AutoCurriculum.Middleware
{
    public static class PostLoginRedirect
    {
        /// <summary>
        /// Trả về URL redirect phù hợp dựa theo role của user sau khi đăng nhập
        /// </summary>
        public static string GetUrl(IList<string> roles, string? returnUrl = null)
        {
            // Nếu có returnUrl hợp lệ thì ưu tiên dùng
            if (!string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith("/"))
                return returnUrl;

            if (roles.Contains("Admin"))
                return "/Admin/Dashboard";

            // Mặc định → trang chủ User
            return "/Home/Index";
        }
    }
}