using System;
using System.Net.Sockets;

public class Client
{
  private Socket socket;
  private double adcRate;
  private int cicRate;
  public double dt;
  private long lastDelay;
  private int lastRead;
  private long size;

  public Client()
  {
    socket = null;
    adcRate = 125;
    cicRate = 50;
    dt = cicRate * 2 / adcRate;
    lastDelay = 0;
    lastRead = 0;
    size = 0;
  }

  public void Connect(string host)
  {
    if (socket != null) return;
    try
    {
      socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socket.ReceiveTimeout = 1000;
      socket.Connect(host, 1001);
    }
    catch
    {
      Disconnect();
    }
  }

  public bool Connected()
  {
    return socket != null;
  }

  public void Disconnect()
  {
    if (socket == null) return;
    try
    {
      socket.Shutdown(SocketShutdown.Both);
    }
    catch
    {
    }
    finally
    {
      socket.Close();
      socket = null;
    }
  }

  private void SendCommand(long code, long data)
  {
    if (socket == null) return;
    byte[] buffer = BitConverter.GetBytes(code << 60 | data);
    try
    {
      socket.Send(buffer);
    }
    catch
    {
      Disconnect();
    }
  }

  public void SetFreqs(double rx, double tx)
  {
    SendCommand(0, (long)(rx + 0.5));
    SendCommand(1, (long)(tx + 0.5));
  }

  public void SetRates(double adc, int cic)
  {
    adcRate = adc;
    cicRate = cic;
    dt = cic * 2 / adc;
    SendCommand(2, cic);
  }

  public void SetDAC(int level)
  {
    long lvl = (long)(level / 100.0 * 4095 + 0.5);
    SendCommand(3, lvl);
  }

  public void SetLevel(double level)
  {
    long lvl = (long)(level / 100.0 * 32766 + 0.5);
    SendCommand(4, lvl);
  }

  public void SetPin(int pin)
  {
    SendCommand(5, pin);
  }

  public void ClearPin(int pin)
  {
    SendCommand(6, pin);
  }

  public void ClearEvents(double readDelay)
  {
    lastDelay = (long)(readDelay * adcRate + 0.5);
    lastRead = 0;
    size = 0;
    SendCommand(7, 0);
  }

  private void UpdateSize()
  {
    long sz = (long)(lastDelay / (cicRate * 2) + 0.5);
    if (sz > 0) SendCommand(10, (long)lastRead << 40 | (sz - 1));
    if (lastRead > 0) size += sz;
    lastDelay = 0;
    lastRead = 0;
  }

  public void AddEvent(double delay, int sync = 0, int gate = 0, int read = 0, double level = 0, double txPhase = 0, double rxPhase = 0)
  {
    long dly = (long)(delay * adcRate + 0.5);
    long lvl = (long)(level / 100.0 * 32766 + 0.5);
    long txp = (long)(txPhase / 360.0 * 0x3FFFFFFF + 0.5);
    long rxp = (long)(rxPhase / 360.0 * 0x3FFFFFFF + 0.5);
    SendCommand(8, lvl << 44 | (long)gate << 41 | (long)sync << 40 | (dly - 1));
    SendCommand(9, rxp << 30 | txp);
    if (lastRead == read)
    {
      lastDelay += dly;
    }
    else
    {
      UpdateSize();
      lastDelay = dly;
      lastRead = read;
    }
  }

  public int[] ReadData()
  {
    int n, offset;
    long limit;
    byte[] buffer;
    int[] result;
    UpdateSize();
    try
    {
      buffer = new byte[65536];
      result = new int[size * 4];
    }
    catch
    {
      return new int[0];
    }
    if (socket == null) return result;
    SendCommand(11, size);
    offset = 0;
    limit = size * 16;
    while (offset < limit)
    {
      try
      {
        n = socket.Receive(buffer);
      }
      catch
      {
        Disconnect();
        break;
      }
      if (n == 0) break;
      Buffer.BlockCopy(buffer, 0, result, offset, n);
      offset += n;
    }
    return result;
  }
}
