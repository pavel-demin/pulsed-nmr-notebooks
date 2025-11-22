using System;
using System.Net.Sockets;
using System.Collections.Generic;

#if WINDOWS
using System.Windows.Forms;
#endif

internal struct Event
{
  public int read;
  public long size;

  public Event(int rd, long sz)
  {
    read = rd;
    size = sz;
  }
}

public class Client
{
  private Socket socket;
  private double adcRate = 125;
  private int cicRate = 50;
  private long lastDelay = 0;
  private int lastRead = 0;
  private long size = 0;
  private readonly List<Event> evts = new List<Event>();

  public void Connect(string host)
  {
    IAsyncResult result;
    if (socket != null) return;
    try
    {
      socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
      socket.SendTimeout = 1000;
      socket.ReceiveTimeout = 1000;
      result = socket.BeginConnect(host, 1001, null, null);
      result.AsyncWaitHandle.WaitOne(1000, true);
    }
    catch (Exception e)
    {
      Disconnect();
#if WINDOWS
      MessageBox.Show(e.Message);
#endif
      return;
    }

    if (!socket.Connected)
    {
      Disconnect();
#if WINDOWS
      MessageBox.Show("Failed to connect to server");
#endif
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

  public void SetFreqs(double tx, double rx)
  {
    SendCommand(0, (long)(rx + 0.5) << 30 | (long)(tx + 0.5));
  }

  public void SetRates(double adc, int cic)
  {
    adcRate = adc;
    cicRate = cic;
    SendCommand(1, cic);
  }

  public void SetDAC(int level)
  {
    long lvl = (long)(level / 100.0 * 4095 + 0.5);
    SendCommand(2, lvl);
  }

  public void SetLevel(double level)
  {
    long lvl = (long)(level / 100.0 * 32766 + 0.5);
    SendCommand(3, lvl);
  }

  public void SetPin(int pin)
  {
    SendCommand(4, pin);
  }

  public void ClearPin(int pin)
  {
    SendCommand(5, pin);
  }

  public void ClearEvents()
  {
    lastDelay = (long)(adcRate * cicRate * 2.0 + 0.5);
    lastRead = 0;
    size = 0;
    evts.Clear();
    SendCommand(6, 0);
  }

  private void UpdateSize()
  {
    long sz = (long)(lastDelay / (cicRate * 2.0) + 0.5);
    if (sz > 0)
    {
      evts.Add(new Event(lastRead, sz));
      SendCommand(9, (long)lastRead << 40 | (sz - 1));
    }
    if (lastRead > 0) size += sz;
    lastDelay = 0;
    lastRead = 0;
  }

#if NET20 || NET35
  public void AddEvent(double delay, int sync, int gate, int read, double level, double txPhase, double rxPhase)
#else
  public void AddEvent(double delay, int sync = 0, int gate = 0, int read = 0, double level = 0, double txPhase = 0, double rxPhase = 0)
#endif
  {
    long dly = (long)(delay * adcRate + 0.5);
    long lvl = (long)(level / 100.0 * 32766 + 0.5);
    long txp = (long)(txPhase / 360.0 * 0x3FFFFFFF + 0.5);
    long rxp = (long)(rxPhase / 360.0 * 0x3FFFFFFF + 0.5);
    SendCommand(7, lvl << 44 | (long)gate << 41 | (long)sync << 40 | (dly - 1));
    SendCommand(8, rxp << 30 | txp);
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

  public float[] ReadTime()
  {
    long i, keep, skip;
    double dt;
    float[] result;
    UpdateSize();
    try
    {
      result = new float[size]; ;
    }
    catch (Exception e)
    {
#if WINDOWS
      MessageBox.Show(e.Message);
#endif
      return new float[0];
    }
    keep = 0;
    skip = 0;
    foreach (Event e in evts)
    {
      if (e.read != 0)
      {
        for (i = 0; i < e.size; i++) result[keep + i] = keep + skip + i;
        keep += e.size;
      }
      else
      {
        skip += e.size;
      }
    }

    dt = cicRate * 2.0 / adcRate;
    for (i = 0; i < result.Length; i++) result[i] *= (float)dt;

    return result;
  }

  public float[] ReadData()
  {
    int n, offset;
    long limit;
    byte[] buffer;
    float[] result;
    UpdateSize();
    try
    {
      buffer = new byte[65536];
      result = new float[size * 4];
    }
    catch (Exception e)
    {
#if WINDOWS
      MessageBox.Show(e.Message);
#endif
      return new float[0];
    }
    if (socket == null) return new float[0];
    SendCommand(10, size);
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
        return new float[0];
      }
      if (n == 0)
      {
        return new float[0];
      }
      Buffer.BlockCopy(buffer, 0, result, offset, n);
      offset += n;
    }
    return result;
  }
}
