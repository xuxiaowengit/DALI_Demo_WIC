using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DALI_Demo_WIC
{
        public partial class MainWindow : Window
        {
            private SerialPort serialPort;
            private DispatcherTimer sendTimer;
            private DispatcherTimer sendTimer2;
            private bool isSendingContinuously = false;
            private DispatcherTimer brightnessTimer;
            private bool isIncreasing = true; // 控制亮度递增或递减

            public MainWindow()
            {
                InitializeComponent();
                InitializeSerialPort();
                InitializeControls();
                InitializeTimer();
                InitializeTimer2();
                InitializeBrightnessTimer(); // 添加这行
            }

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                     //NewLine = "",              // 设置为空
                Encoding = Encoding.ASCII  // 使用ASCII编码
            };
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        private void InitializeControls()
        {
            // 初始化串口列表
            cmbPort.ItemsSource = SerialPort.GetPortNames();
            if (cmbPort.Items.Count > 0)
                cmbPort.SelectedIndex = 0;

            // 绑定事件
            btnConnect.Click += BtnConnect_Click;
            btnSingleSend.Click += BtnSingleSend_Click;
            btnContinuousSend.Click += btnContinuousSend_Click;
            btnCloseSend.Click += btnCloseSend_Click;

            // 绑定参数变化事件
            txtDeviceID.TextChanged += UpdateMessage;
            txtFunctionCode.TextChanged += UpdateMessage;
            txtLampNumber.TextChanged += UpdateMessage;
            txtParamCount.TextChanged += UpdateMessage;
            txtByteCount.TextChanged += UpdateMessage;

            // 绑定灯光参数变化事件
            sliderFade.ValueChanged += UpdateLightParams;
            sliderBrightness.ValueChanged += UpdateLightParams;
            sliderColorTemp.ValueChanged += UpdateLightParams;
            sliderRed.ValueChanged += UpdateLightParams;
            sliderGreen.ValueChanged += UpdateLightParams;
            sliderBlue.ValueChanged += UpdateLightParams;
            sliderWhite.ValueChanged += UpdateLightParams;

            // 验证控件是否启用
            sliderFade.IsEnabled = true;
            sliderBrightness.IsEnabled = true;
            sliderColorTemp.IsEnabled = true;
            sliderRed.IsEnabled = true;
            sliderGreen.IsEnabled = true;
            sliderBlue.IsEnabled = true;
            sliderWhite.IsEnabled = true;


            //滑动块数值处理???/**/
            // 添加滑块值改变事件处理
            sliderFade.ValueChanged += (s, e) => txtFade.Text = ((int)e.NewValue).ToString();
            sliderBrightness.ValueChanged += (s, e) => txtBrightness.Text = ((int)e.NewValue).ToString();
            sliderColorTemp.ValueChanged += (s, e) => txtColorTemp.Text = ((int)e.NewValue).ToString();
            sliderRed.ValueChanged += (s, e) => txtRed.Text = ((int)e.NewValue).ToString();
            sliderGreen.ValueChanged += (s, e) => txtGreen.Text = ((int)e.NewValue).ToString();
            sliderBlue.ValueChanged += (s, e) => txtBlue.Text = ((int)e.NewValue).ToString();
            sliderWhite.ValueChanged += (s, e) => txtWhite.Text = ((int)e.NewValue).ToString();


            // 设置滑块默认值
            sliderFade.Value = 1;
            sliderBrightness.Value = 100;
            sliderColorTemp.Value = 26;
            sliderRed.Value = 110;
            sliderGreen.Value = 130;
            sliderBlue.Value = 150;
            sliderWhite.Value = 200;

            // 设置滑块属性
            foreach (var slider in new[] { sliderFade, sliderBrightness, sliderColorTemp,
                                     sliderRed, sliderGreen, sliderBlue, sliderWhite })
            {
                slider.IsSnapToTickEnabled = true;
                slider.TickFrequency = 1;
            }
            // 绑定值改变事件
       
        }

        // 初始化定时器方法
        private void InitializeTimer()
        {
            sendTimer = new DispatcherTimer();
            sendTimer.Tick += SendTimer_Tick;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                try
                {
                    string data = serialPort.ReadExisting();
                    //txtStatus.Text = $"收到数据: {data}";
                    // 转换为字节数组
                    byte[] bytes = Encoding.ASCII.GetBytes(data);

                    // 转换为16进制字符串
                    string hexString = BitConverter.ToString(bytes).Replace("-", " ");

                    // 更新状态显示
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = $"收到数据: {hexString}";
                    });
                }
                catch (Exception ex)
                {
                    //txtStatus.Text = $"接收错误: {ex.Message}";
                    this.Dispatcher.Invoke(() =>
                    {
                        txtStatus.Text = $"数据转换错误: {ex.Message}";
                    });
                }
            });
        }


        // 在窗口关闭事件中处理资源释放
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (sendTimer2 != null)
            {
                sendTimer2.Stop();
                isSendingContinuously = false;
            }
            base.OnClosing(e);
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    btnConnect.Content = "连接";
                    txtStatus.Text = "串口已断开";
                    btnConnect.Style = (Style)FindResource("DisconnectButtonStyle");
                }
                else
                {
                    serialPort.PortName = cmbPort.SelectedItem.ToString();
                    serialPort.BaudRate = int.Parse((cmbBaudRate.SelectedItem as ComboBoxItem).Content.ToString());
                    serialPort.Open();
                    btnConnect.Content = "断开";
                    btnConnect.Style = (Style)FindResource("ConnectButtonStyle");
                    //写出状态
                    txtStatus.Text = $"已连接 {serialPort.PortName} @ {serialPort.BaudRate}";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"连接错误: {ex.Message}";
            }
        }

        private void UpdateMessage(object sender, TextChangedEventArgs e)
        {
            GenerateMessage();
        }

        private void UpdateLightParams(object sender, RoutedEventArgs e)
        {
            GenerateMessage();
        }

        private void GenerateMessage()
        {
            try
            {
                StringBuilder message = new StringBuilder();

                // 添加报文前缀
                message.Append(txtDeviceID.Text.PadLeft(2, '0'));
                message.Append(txtFunctionCode.Text.PadLeft(2, '0'));
                message.Append(txtLampNumber.Text.PadLeft(4, '0'));
                message.Append(txtParamCount.Text.PadLeft(4, '0'));
                message.Append(txtByteCount.Text.PadLeft(2, '0'));

                // 添加灯光参数
                message.Append(sliderFade.Value.ToString("X2"));
                message.Append(sliderBrightness.Value.ToString("X2"));
                message.Append(sliderColorTemp.Value.ToString("X2"));
                message.Append(sliderRed.Value.ToString("X2"));
                message.Append(sliderGreen.Value.ToString("X2"));
                message.Append(sliderBlue.Value.ToString("X2"));
                message.Append(sliderWhite.Value.ToString("X2"));

                // 计算CRC16
                string crc = CalculateCRC16(message.ToString());
                message.Append(crc);

                txtFullMessage.Text = message.ToString();
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"生成报文错误: {ex.Message}";
            }
        }
 
        private void BtnSingleSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        //private void BtnContinuousSend_Click(object sender, RoutedEventArgs e)
        //{
        //    if (isSendingContinuously)
        //    {
        //        sendTimer.Stop();
        //        btnContinuousSend.Content = "连续发送";
        //        isSendingContinuously = false;
        //    }
        //    else
        //    {
        //        if (int.TryParse(txtInterval.Text, out int interval) && interval > 0)
        //        {
        //            sendTimer.Interval = TimeSpan.FromMilliseconds(interval);
        //            sendTimer.Start();
        //            btnContinuousSend.Content = "停止发送";
        //            isSendingContinuously = true;
        //        }
        //        else
        //        {
        //            txtStatus.Text = "请输入有效的发送间隔时间";
        //        }
        //    }
        //}

        private void SendTimer_Tick(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine(txtFullMessage.Text);
                    txtStatus.Text = $"报文已发送: {txtFullMessage.Text}";
                }
                else
                {
                    txtStatus.Text = "请先连接串口";
                }
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"发送错误: {ex.Message}";
            }
        }


        private void sliderFade_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

      
        private void txtFade_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderBrightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderColorTemp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderRed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderGreen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderBlue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }

        private void sliderWhite_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 获取对应的TextBox
                string textBoxName = "txt" + slider.Name.Substring(6); // 去掉"slider"前缀
                var textBox = this.FindName(textBoxName) as TextBox;

                if (textBox != null)
                {
                    // 更新TextBox的值，转换为整数
                    textBox.Text = ((int)slider.Value).ToString();
                }
            }
        }


        private string ValidateInput(string input)
        {
            return string.IsNullOrEmpty(input) ? "00" : input.PadLeft(2, '0');
        }

        private string DecimalToHex(string decimalStr, int maxValue)
        {
            if (string.IsNullOrEmpty(decimalStr))
                return "00";

            if (int.TryParse(decimalStr, out int decimalValue))
            {
                // 确保值在有效范围内
                decimalValue = Math.Min(Math.Max(0, decimalValue), maxValue);
                return decimalValue.ToString("X2"); // 转为2位16进制
            }
            return "00";
        }

        private void txtByteCount_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Control_ValueChanged(object sender, EventArgs e)
        {
              Console.WriteLine("Control Value Changed RUNNING");
            UpdateFullMessage();
        }

        private string CalculateCRC16(string message)
        {
            try
            {
                // 去除空格,确保16进制字符串格式正确
                message = message.Replace(" ", "");

                ushort crc = 0xFFFF;
                byte[] bytes = new byte[message.Length / 2];

                // 转换16进制字符串为字节数组
                for (int i = 0; i < message.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(message.Substring(i, 2), 16);
                }

                // CRC16/MODBUS 计算
                foreach (byte b in bytes)
                {
                    crc ^= b;
                    for (int j = 0; j < 8; j++)
                    {
                        if ((crc & 0x0001) != 0)
                        {
                            crc >>= 1;
                            crc ^= 0xA001;
                        }
                        else
                        {
                            crc >>= 1;
                        }
                    }
                }

                // 返回反转后的CRC值 (低字节在前，高字节在后)
                //return string.Format("{0:X2}{1:X2}", (byte)((crc >> 8) & 0xFF), (byte)(crc & 0xFF));

                // 修改返回顺序：低字节在前，高字节在后
                return string.Format("{0:X2}{1:X2}", (byte)(crc & 0xFF), (byte)((crc >> 8) & 0xFF));

            }
            catch (Exception ex)
            {
                MessageBox.Show($"CRC计算错误: {ex.Message}");
                return "0000";
            }
        }

        private void Control_ValueChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == txtInterval)
            {
                UpdateBrightnessInterval(txtInterval.Text);
            }
            UpdateFullMessage();
        }


        // 添加CheckBox事件处理
        private void chkBrightnessAuto_Click(object sender, RoutedEventArgs e)
        {
            if (chkBrightnessAuto.IsChecked == true)
            {
                brightnessTimer.Start();
            }
            else
            {
                brightnessTimer.Stop();
            }
        }

      
        //递增亮度
        private void InitializeBrightnessTimer()
        {
            brightnessTimer = new DispatcherTimer();
            brightnessTimer.Interval = TimeSpan.FromMilliseconds(100);
            brightnessTimer.Tick += BrightnessTimer_Tick;
        }

        private void BrightnessTimer_Tick(object sender, EventArgs e)
        {
            if (isIncreasing)
            {
                if (sliderBrightness.Value < sliderBrightness.Maximum)
                    sliderBrightness.Value = sliderBrightness.Value+127;
                else
                    isIncreasing = false;
            }
            else
            {
                if (sliderBrightness.Value > sliderBrightness.Minimum)
                    sliderBrightness.Value = sliderBrightness.Value - 127;
                else
                    isIncreasing = true;
            }
            UpdateFullMessage();
        }

        private void chkBrightnessAuto_Checked(object sender, RoutedEventArgs e)
        {
            //brightnessTimer.Start();
            if (int.TryParse(txtInterval.Text, out int interval) && interval >= 10)
            {
                brightnessTimer.Interval = TimeSpan.FromMilliseconds(interval);
                brightnessTimer.Start();
            }
            else
            {
                MessageBox.Show("请输入有效的间隔时间（>=10ms）");
                if (chkBrightnessAuto != null)
                {
                    chkBrightnessAuto.IsChecked = false;
                }
            }
        }

        private void chkBrightnessAuto_Unchecked(object sender, RoutedEventArgs e)
        {
            brightnessTimer.Stop();
        }


        private void UpdateFullMessage()
        {
            try
            {
                // 检查控件是否初始化
                if (txtDeviceID == null || txtFunctionCode == null || txtLampNumber == null ||
                    txtParamCount == null || txtByteCount == null || txtFade == null ||
                    txtBrightness == null || txtColorTemp == null || txtRed == null ||
                    txtGreen == null || txtBlue == null || txtWhite == null ||
                    txtFullMessage == null)
                {
                    return; // 如果有控件未初始化则退出
                }

                // 获取并验证输入值
                string deviceId = ValidateInput(txtDeviceID.Text);
                string functionCode = ValidateInput(txtFunctionCode.Text);
                string lightAddress = ValidateInput(txtLampNumber.Text);
                string paramCount = ValidateInput(txtParamCount.Text);
                string paramBytes = ValidateInput(txtByteCount.Text);

                // 获取并转换滑块值
                string fade = DecimalToHex(txtFade.Text, 15);
                string brightness = DecimalToHex(txtBrightness.Text, 254);
                string colorTemp = DecimalToHex(txtColorTemp.Text, 65);
                string red = DecimalToHex(txtRed.Text, 254);
                string green = DecimalToHex(txtGreen.Text, 254);
                string blue = DecimalToHex(txtBlue.Text, 254);
                string white = DecimalToHex(txtWhite.Text, 254);

                // 组合报文
                string message = $"{deviceId}{functionCode}{lightAddress}{paramCount}{paramBytes}" +
                                $"00{fade}00{brightness}00{colorTemp}00{red}00{green}00{blue}00{white}";

                // 计算CRC16
                string crc16 = CalculateCRC16(message);
                string fullMessage = message + crc16;

                // 更新显示
                txtFullMessage.Text = fullMessage.ToUpper();

                //chkBrightnessAuto_Checked();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新报文时发生错误：{ex.Message}");
            }
        }


        // 单次发送方法
        private void btnSingleSend_Click(object sender, RoutedEventArgs e)
        {
            UpdateFullMessage();

            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    string message = txtFullMessage.Text.Replace(" ", "");
                    byte[] bytes = new byte[message.Length / 2];
                    for (int i = 0; i < message.Length; i += 2)
                    {
                        bytes[i / 2] = Convert.ToByte(message.Substring(i, 2), 16);
                    }
                    serialPort.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    StopContinuousSending();
                    MessageBox.Show("串口已关闭，停止发送！");
                }
            }
            catch (Exception ex)
            {
                StopContinuousSending();
                MessageBox.Show($"发送失败：{ex.Message}");
            }


        }

        // 初始化定时器方法
        private void InitializeTimer2()
        {
            sendTimer2 = new DispatcherTimer();
            sendTimer2.Tick += SendTimer_Tick2;
        }


        // 定时器发送事件
 

        private void SendTimer_Tick2(object sender, EventArgs e)
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    string message = txtFullMessage.Text?.Replace(" ", "");

                    // 验证16进制字符串格式
                    if (string.IsNullOrEmpty(message) || !IsValidHexString(message))
                    {
                        StopContinuousSending();
                        MessageBox.Show("无效的16进制数据格式！");
                        return;
                    }

                    byte[] bytes = new byte[message.Length / 2];
                    for (int i = 0; i < message.Length; i += 2)
                    {
                        bytes[i / 2] = Convert.ToByte(message.Substring(i, 2), 16);
                    }
                    serialPort.Write(bytes, 0, bytes.Length);
                }
                else
                {
                    StopContinuousSending();
                }
            }
            catch (Exception ex)
            {
                StopContinuousSending();
                Console.WriteLine($"发送异常: {ex.Message}");
            }
        }

        private bool IsValidHexString(string hexString)
        {
            if (hexString.Length % 2 != 0) return false;

            try
            {
                return hexString.All(c => "0123456789ABCDEFabcdef".Contains(c));
            }
            catch
            {
                return false;
            }
        }




        // 连续发送按钮事件 btnContinuousSend_Click
        private void btnContinuousSend_Click(object sender, RoutedEventArgs e)
        {

            bool currentState = isSendingContinuously;
            Console.WriteLine($"当前COM状态: {currentState}");
            // 调试输出当前状态
            Console.WriteLine($"当前状态: {isSendingContinuously}");
            if (isSendingContinuously)
            {
                Console.WriteLine($"停止连续发送{isSendingContinuously}");
                StopContinuousSending();

               
            } else{
                Console.WriteLine($"启动连续发送{isSendingContinuously}");
                UpdateFullMessage();
                StartContinuousSending();
                
            }
        }

        //private void StartContinuousSending()
        //{
        //    try
        //    {
        //        if (!int.TryParse(txtInterval.Text, out int interval) || interval < 10)
        //    {
        //        MessageBox.Show("请输入有效的发送间隔（>=10ms）");
        //        return;
        //    }

        //    isSendingContinuously = true;
        //    btnContinuousSend.Style = (Style)FindResource("ConnectButtonStyle");
        //    btnContinuousSend.Content = "停止发送";
        //    sendTimer2.Interval = TimeSpan.FromMilliseconds(interval|10);
        //    sendTimer2.Start();

        //}
        //    catch (Exception ex)
        //    {
        //        isSendingContinuously = false;
        //        MessageBox.Show($"启动连续发送失败: {ex.Message}");
        //    }
        //}


        private void StartContinuousSending()
        {
            try
            {
                if (!int.TryParse(txtInterval.Text, out int interval) || interval < 10)
                {
                    MessageBox.Show("请输入有效的发送间隔（>=10ms）");
                    return;
                }

                // 设置最小间隔
                interval = Math.Max(interval, 10);

                isSendingContinuously = true;
                btnContinuousSend.Style = (Style)FindResource("ConnectButtonStyle");
                btnContinuousSend.Content = "停止发送";
                sendTimer2.Interval = TimeSpan.FromMilliseconds(interval);
                sendTimer2.Start();

                Console.WriteLine($"定时器启动 - 间隔: {interval}ms");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动连续发送失败: {ex.Message}");
                isSendingContinuously = false;
            }
        }


        private void StopContinuousSending()
        {
            try
            {
                isSendingContinuously = false;
            btnContinuousSend.Content = "连续发送";
            btnContinuousSend.Style = (Style)FindResource("DisconnectButtonStyle");
            sendTimer2.Interval = TimeSpan.FromMilliseconds(0);
            sendTimer2.Stop();
                serialPort.Close();
            }
             catch (Exception ex)
            {
                MessageBox.Show($"停止连续发送失败: {ex.Message}");
            }
        }

        //更新频率触发发送
        private void UpdateBrightnessInterval(string intervalText)
        {
            if (brightnessTimer != null && chkBrightnessAuto.IsChecked == true)
            {
                if (int.TryParse(intervalText, out int interval) && interval >= 10)
                {
                    brightnessTimer.Interval = TimeSpan.FromMilliseconds(interval);
                }
            }
        }

        private void btnCloseSend_Click(object sender, RoutedEventArgs e)
        {
            serialPort.Close();
            btnConnect.Content = "连接";
            txtStatus.Text = "串口已断开";
            btnConnect.Style = (Style)FindResource("DisconnectButtonStyle");
            isSendingContinuously = false;
            btnContinuousSend.Content = "连续发送";
            btnContinuousSend.Style = (Style)FindResource("DisconnectButtonStyle");
            sendTimer2.Interval = TimeSpan.FromMilliseconds(0);
            sendTimer2.Stop();
        }
    }
}
