module PulsedNMR

using Sockets

export Client,
    connect!,
    disconnect!,
    set_freqs!,
    set_rates!,
    set_dac!,
    set_level!,
    set_pin!,
    clear_pin!,
    clear_events!,
    add_event!,
    read_data!

mutable struct Client
    # network socket
    socket::TCPSocket
    # ADC sample rate
    adc_rate::Real
    # CIC decimation rate
    cic_rate::Int64
    # time between two RX samples
    dt::Real
    # total delay
    lastDelay::Int64
    # read flag of the last event
    lastRead::Int64
    # total number of RX samples
    size::Int64
    Client() = new(TCPSocket(), 125, 50, 0.8, 0, 0, 0)
end

function connect!(c::Client, host::String)
    c.socket = Sockets.connect(host, 1001)
    nothing
end

function disconnect!(c::Client)
    try
        close(c.socket)
    finally
        c.socket = TCPSocket()
    end
end

function _send_command(c::Client, code::Int64, data::Int64)
    write(c.socket, code << 60 | data)
    nothing
end

function set_freqs!(c::Client; tx::Real, rx::Real)
    _send_command(c, 0, round(Int64, rx))
    _send_command(c, 1, round(Int64, tx))
end

function set_rates!(c::Client; adc::Real, cic::Int64)
    c.adc_rate = adc
    c.cic_rate = cic
    c.dt = cic * 2 / adc
    _send_command(c, 2, cic)
end

function set_dac!(c::Client; level::Real)
    lvl = round(Int64, level / 100.0 * 4095)
    _send_command(c, 3, lvl)
end

function set_level!(c::Client; level::Real)
    lvl = round(Int64, level / 100.0 * 32766)
    _send_command(c, 4, lvl)
end

function set_pin!(c::Client; pin::Int64)
    _send_command(c, 5, pin)
end

function clear_pin!(c::Client; pin::Int64)
    _send_command(c, 6, pin)
end

function clear_events!(c::Client; read_delay::Real=0)
    c.lastDelay = round(Int64, read_delay * c.adc_rate)
    c.lastRead = 0
    c.size = 0
    _send_command(c, 7, 0)
end

function _update_size!(c::Client)
    sz = round(Int64, c.lastDelay / (c.cic_rate * 2))

    if sz > 0
        _send_command(c, 10, c.lastRead << 40 | (sz - 1))
    end

    if c.lastRead != 0
        c.size += sz
    end

    c.lastDelay = 0
    c.lastRead = 0
end

function add_event!(c::Client, delay::Real;
    sync::Int64=0, gate::Int64=0, read::Int64=0,
    level::Real=0, tx_phase::Real=0, rx_phase::Real=0)

    dly = round(Int64, delay * c.adc_rate)
    lvl = round(Int64, level / 100.0 * 32766)
    txp = round(Int64, tx_phase / 360.0 * 0x3fffffff)
    rxp = round(Int64, rx_phase / 360.0 * 0x3fffffff)

    _send_command(c, 8, lvl << 44 | gate << 41 | sync << 40 | (dly - 1))
    _send_command(c, 9, rxp << 30 | txp)

    if c.lastRead == read
        c.lastDelay += dly
    else
        _update_size!(c)
        c.lastDelay = dly
        c.lastRead = read
    end
end

function read_data!(c::Client)
    _update_size!(c)

    _send_command(c, 11, c.size)

    data = read(c.socket, c.size * 16)

    reinterpret(ComplexF32, data)
end

end # module PulsedNMR
