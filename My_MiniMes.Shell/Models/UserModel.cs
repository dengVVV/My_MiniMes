namespace My_MiniMes.Shell.Models
{
    /// <summary>
    /// 用户实体类 (数据库映射模型)
    /// 纯粹的贫血模型，属性不需要包含业务逻辑或通知。
    /// </summary>
    public class UserModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Role { get; set; } = 1; // 1: admin, 2: leader, 3: employee
        public string Account { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        public string? Email { get; set; }
        public byte IsActive { get; set; } = 1;
    }
}
