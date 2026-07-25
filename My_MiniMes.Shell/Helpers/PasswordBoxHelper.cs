using System.Windows;
using System.Windows.Controls;

namespace My_MiniMes.Shell.Helpers
{
    /// <summary>
    /// 【保姆级解析：为什么我们需要这个类？】
    /// 1. WPF 原生的 TextBox 控件有一个 Text 属性，它是 DependencyProperty (依赖属性)，
    /// 所以你可以在 XAML 里面写 Text="{Binding xxx}"。
    /// 2. 但是，WPF 原生的 PasswordBox 控件有一个 Password 属性，出于微软的安全考量（防止密码在内存里被黑客直接读取），
    /// 它不是依赖属性！
    /// 3. 不是依赖属性的后果是：你不能在 XAML 里写 Password="{Binding xxx}"，编译器会直接报错。
    /// 4. 那么，在要求“前后端完全分离”的 MVVM 架构中，前端密码框输入了什么，怎么传给后端的 ViewModel 呢？
    /// 
    /// 【解决方案：附加属性 (Attached Property)】
    /// 我们利用 WPF 的“附加属性”机制，相当于给原生 PasswordBox “外挂”了一个支持绑定的属性
    /// （就叫 helper:PasswordBoxHelper.Password）。
    /// 当外挂属性改变时，我们通过代码操作原生的 Password；当用户在界面敲击密码时，原生 Password 改变，我们去同步更新外挂属性，
    /// 最终传给 ViewModel。
    /// (在 Prism 框架中，通常会使用 Interaction.Behaviors 来写一个 PasswordBehavior 实现一样的效果。)
    /// </summary>
    public static class PasswordBoxHelper
    {
        // =========================================================================
        // 第一部分：注册外挂的 Password 属性，让 XAML 可以写 {Binding}
        // =========================================================================
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password", typeof(string), 
                typeof(PasswordBoxHelper), 
                new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        // Get 和 Set 方法是 WPF 附加属性的标准模板代码
        public static string GetPassword(DependencyObject dp) => 
            (string)dp.GetValue(PasswordProperty);
        public static void SetPassword(DependencyObject dp, string value) => 
            dp.SetValue(PasswordProperty, value);

        // 这是一个防止死循环的标记。因为 A(界面) 变了会去改 B(后端)，B(后端)变了又会回来改 A(界面)，陷入无限死循环。
        private static bool _isUpdating;
        
        /// <summary>
        /// 当后端的 ViewModel 的密码值发生了改变时，触发此方法，把它推给界面的 PasswordBox
        /// </summary>
        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                // 先取消订阅界面的事件，防止接下来我们手动修改界面的值时，又触发了界面的事件，导致死循环
                passwordBox.PasswordChanged -= PasswordChanged;
                
                if (!_isUpdating) 
                {
                    // 把后端的新值赋给界面的原生 Password
                    passwordBox.Password = (e.NewValue == null ? string.Empty : e.NewValue.ToString())!;
                }
                
                // 恢复订阅界面的事件
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        // =========================================================================
        // 第二部分：注册外挂的 Attach 属性，作为一个“开关”，激活功能
        // =========================================================================
        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach", typeof(bool),
                typeof(PasswordBoxHelper), new FrameworkPropertyMetadata(false, Attach));

        public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);
        public static void SetAttach(DependencyObject dp, bool value) => dp.SetValue(AttachProperty, value);

        /// <summary>
        /// 当在 XAML 中写了 helper:PasswordBoxHelper.Attach="True" 时，触发此方法。
        /// 它的作用是把 WPF 原生的 PasswordChanged 事件挂载到我们自定义的方法上。
        /// </summary>
        private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                if ((bool)e.NewValue) 
                {
                    // 如果开关打开，订阅原生事件
                    passwordBox.PasswordChanged += PasswordChanged;
                }
                else 
                {
                    // 开关关闭，取消订阅
                    passwordBox.PasswordChanged -= PasswordChanged;
                }
            }
        }

        // =========================================================================
        // 第三部分：桥接核心逻辑
        // =========================================================================
        
        /// <summary>
        /// 当用户在界面上按键盘输入密码时，原生 PasswordBox 会触发这个事件。
        /// 我们就在这里，把界面的真实密码，同步到我们的外挂 Password 属性上（继而传给 ViewModel）
        /// </summary>
        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _isUpdating = true; // 标记我们正在更新，防止死循环
                
                // 将真实的 passwordBox.Password 同步到我们注册的附加属性中，触发 MVVM 的 Binding，送到 ViewModel。
                SetPassword(passwordBox, passwordBox.Password);
                
                _isUpdating = false;
            }
        }
    }
}
