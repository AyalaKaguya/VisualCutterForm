namespace VisualCutterForm.Lib
{
    public enum UserRole
    {
        None = 0,
        Operator = 1,    // 用户: 查看日志、手动执行流程
        Engineer = 2,    // 工程师: 操作员权限 + 相机/串口设置
        Admin = 3,       // 管理员: 全部权限
    }
}
