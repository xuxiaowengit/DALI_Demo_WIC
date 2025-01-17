using System;
using System.IO.Ports;
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
        private bool isSendingContinuously = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSerialPort();
            InitializeControls();
            InitializeTimer();
        }

        private void InitializeSerialPort()
        {
            serialPort = new SerialPort
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None
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
            btnContinuousSend.Click += BtnContinuousSend_Click;

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
                    txtStatus.Text = $"收到数据: {data}";
                }
                catch (Exception ex)
                {
                    txtStatus.Text = $"接收错误: {ex.Message}";
                }
            });
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
                }
                else
                {
                    serialPort.PortName = cmbPort.SelectedItem.ToString();
                    serialPort.BaudRate = int.Parse((cmbBaudRate.SelectedItem as ComboBoxItem).Content.ToString());
                    serialPort.Open();
                    btnConnect.Content = "断开";
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

        private string CalculateCRC16(string data)
        {
            ushort crc = 0xFFFF;
            foreach (char c in data)
            {
                crc ^= (ushort)(c << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc.ToString("X4");
        }

        private void BtnSingleSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void BtnContinuousSend_Click(object sender, RoutedEventArgs e)
        {
            if (isSendingContinuously)
            {
                sendTimer.Stop();
                btnContinuousSend.Content = "连续发送";
                isSendingContinuously = false;
            }
            else
            {
                if (int.TryParse(txtInterval.Text, out int interval) && interval > 0)
                {
                    sendTimer.Interval = TimeSpan.FromMilliseconds(interval);
                    sendTimer.Start();
                    btnContinuousSend.Content = "停止发送";
                    isSendingContinuously = true;
                }
                else
                {
                    txtStatus.Text = "请输入有效的发送间隔时间";
                }
            }
        }

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

        private void btnSingleSend_Click_1(object sender, RoutedEventArgs e)
        {

        }
        private void btnSingleSend_Click_2(object sender, RoutedEventArgs e)
        {

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

        private void txtFullMessage_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
