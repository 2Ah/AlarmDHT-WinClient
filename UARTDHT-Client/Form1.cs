using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace UARTDHT_Client
{
  public partial class Form1 : Form
  {
    bool isAllTimerEnd = true; // Для блокирования процедуры таймера, если предыдущая еще не завершена. 

    public Form1()
    {
      InitializeComponent();
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
      mskTCP.Enabled = false;
      numPort.Enabled = false;
      btnConnect.Enabled = false;
      timerServerRequest.Enabled = true;
      notifyIcon1.Icon = Properties.Resources.HT_16;
    }

    private void btnDisconnect_Click(object sender, EventArgs e)
    {
      timerServerRequest.Enabled = false;
      mskTCP.Enabled = true;
      numPort.Enabled = true;
      btnConnect.Enabled = true;
      labInfoConnect.Text = "Соединение завершено.";
      labInfoDHT.Text = "";
      notifyIcon1.Icon = Properties.Resources.HT_16_bw;
    }

    private void timerServerRequest_Tick(object sender, EventArgs e)
    {
      if (!isAllTimerEnd) return;
      isAllTimerEnd = false;
      int serverPort = (int)numPort.Value;
      string message = DateTime.Now.ToLongTimeString() + ": ",
        serverAddress = mskTCP.Text.Replace(" ", "");
      labInfoConnect.Text = "";
      labInfoDHT.Text = "";

      TcpClient client = new TcpClient();
      NetworkStream stream = null;
      try
      {
        labInfoConnect.Text = "Подключение к серверу на " + serverAddress + ":" + serverPort;
        client.Connect(IPAddress.Parse(serverAddress), serverPort);
        labInfoConnect.Text += "\nПодключение установлено.";
        stream = client.GetStream();
        stream.ReadTimeout = 999; // Через 1 сек. ожидания объектом NetworkStream ответного сообщения от сервера, генерируем Exception.
        byte[] data = Encoding.Unicode.GetBytes("DHTClient"); // преобразуем сообщение в массив байтов 
        stream.Write(data, 0, data.Length); // отправка сообщения  

        data = new byte[256]; // Буфер для получаемых данных (символ Unicode - 2 байта).
        int bytes = stream.Read(data, 0, data.Length); // Читаем первые 256 байтов сообщения.
        message += Encoding.Unicode.GetString(data, 0, bytes);

        labInfoDHT.Text = message;
      }
      catch (SocketException ex)
      {
        if (ex.ErrorCode == 10060)
        {
          message += "SocketException:\nОт хоста " + serverAddress + ":" + serverPort + " нет ответа.";
          btnDisconnect.PerformClick();
        }
        else if (ex.ErrorCode == 10061) message += "SocketException:\nПо адресу " + serverAddress + ":" + serverPort + " отвергнуто подключение.";
        else message += "SocketException: " + ex;
        labInfoConnect.Text = message;
      }
      catch (Exception ex)
      {
        message += "Exception: " + ex;
        labInfoConnect.Text = message;
      }
      finally
      {
        if (stream != null) stream.Close();
        if (client != null) client.Close();
        TheBalloonatic(message);
        isAllTimerEnd = true;
      }
    }

    private void TheBalloonatic(string message)
    {
      bool isPanic = false;
      if (message.IndexOf("Exception:") >= 0) isPanic = true; // + "SocketException:"
      else if (message.IndexOf("Датчик на сервере не обнаружен") >= 0) isPanic = true;
      else if (message.IndexOf("Ошибка преобразования") >= 0) isPanic = true;
      else if (message.IndexOf("Критическая ") >= 0) isPanic = true;
      //MessageBox.Show(message);
      if (isPanic)
      {
        notifyIcon1.BalloonTipTitle = "UARTDHT-Client";
        notifyIcon1.BalloonTipText = message;
        notifyIcon1.ShowBalloonTip(2000);
      }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
      if (WindowState == FormWindowState.Minimized) Hide();
    }

    private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
    {
      Show();
      WindowState = FormWindowState.Normal;
    }

    private void Form1_Shown(object sender, EventArgs e)
    { //Загрузка конфигурации из директории программы. 
      bool isConnect = false;
      System.Data.DataSet ds = new System.Data.DataSet();
      try
      {
        ds.ReadXmlSchema(Application.StartupPath + "\\UARTDHT-Client.cfg");
        mskTCP.Text = (string)ds.ExtendedProperties["tcp"];
        numPort.Value = decimal.Parse((string)ds.ExtendedProperties["tcpPort"]);
        isConnect = Convert.ToBoolean(ds.ExtendedProperties["isConnect"]);
      }
      catch { }
      if (isConnect) btnConnect.PerformClick();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    { //Сохранение конфигурации в директории программы.
      System.Data.DataSet ds = new System.Data.DataSet();
      ds.ExtendedProperties["tcp"] = mskTCP.Text;
      ds.ExtendedProperties["tcpPort"] = numPort.Value;
      ds.ExtendedProperties["isConnect"] = timerServerRequest.Enabled;
      ds.WriteXmlSchema(Application.StartupPath + "\\UARTDHT-Client.cfg");
    }
  }
}
