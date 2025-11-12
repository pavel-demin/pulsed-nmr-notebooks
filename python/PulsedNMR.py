from socket import create_connection
from struct import pack
import numpy as np


class Client:
    def __init__(self):
        # network socket
        self.socket = None
        # ADC sample rate
        self.adc_rate = 125
        # CIC decimation rate
        self.cic_rate = 50
        # time between two RX samples
        self.dt = self.cic_rate * 2 / self.adc_rate
        # total delay
        self.lastDelay = 0
        # read flag of the last event
        self.lastRead = 0
        # total number of RX samples
        self.size = 0

    def connect(self, host):
        self.socket = create_connection((host, 1001), timeout=1)
        self.socket.settimeout(None)

    def disconnect(self):
        self.socket.close()

    def send_command(self, code, data):
        self.socket.sendall(pack("<Q", int(code) << 60 | int(data)))

    def set_freqs(self, rx, tx):
        self.send_command(0, rx)
        self.send_command(1, tx)

    def set_rates(self, adc, cic):
        self.adc_rate = adc
        self.cic_rate = cic
        self.dt = cic * 2 / adc
        self.send_command(2, cic)

    def set_dac(self, level):
        lvl = int(level / 100.0 * 4095 + 0.5)
        self.send_command(3, lvl)

    def set_level(self, level):
        lvl = int(level / 100.0 * 32766 + 0.5)
        self.send_command(4, lvl)

    def set_pin(self, pin):
        self.send_command(5, pin)

    def clear_pin(self, pin):
        self.send_command(6, pin)

    def clear_events(self, read_delay=0):
        self.lastDelay = int(read_delay * self.adc_rate + 0.5)
        self.lastRead = 0
        self.size = 0
        self.send_command(7, 0)

    def update_size(self):
        sz = int(self.lastDelay / (self.cic_rate * 2) + 0.5)
        if sz > 0:
            self.send_command(10, self.lastRead << 40 | int(sz - 1))
        if self.lastRead:
            self.size += sz
        self.lastDelay = 0
        self.lastRead = 0

    def add_event(self, delay, sync=0, gate=0, read=0, level=0, tx_phase=0, rx_phase=0):
        dly = int(delay * self.adc_rate + 0.5)
        lvl = int(level / 100.0 * 32766 + 0.5)
        txp = int(tx_phase / 360.0 * 0x3FFFFFFF + 0.5)
        rxp = int(rx_phase / 360.0 * 0x3FFFFFFF + 0.5)
        self.send_command(8, lvl << 44 | gate << 41 | sync << 40 | (dly - 1))
        self.send_command(9, rxp << 30 | txp)
        if self.lastRead == read:
            self.lastDelay += dly
        else:
            self.update_size()
            self.lastDelay = dly
            self.lastRead = read

    def read_data(self):
        self.update_size()

        data = np.empty(self.size * 4, np.int32)
        view = data.view(np.uint8)

        self.send_command(11, self.size)

        offset = 0
        limit = view.size
        while offset < limit:
            buffer = self.socket.recv(limit - offset)
            size = len(buffer)
            view[offset : offset + size] = np.frombuffer(buffer, np.uint8)
            offset += size

        return data.astype(np.float32).view(np.complex64) / 2**30
