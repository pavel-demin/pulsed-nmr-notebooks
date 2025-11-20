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
    read_time!,
    read_data!

mutable struct Client
    # network socket
    socket::TCPSocket
    # ADC sample rate
    adc_rate::Real
    # CIC decimation rate
    cic_rate::Int64
    # total delay
    last_delay::Int64
    # read flag of the last event
    last_read::Int64
    # total number of RX samples
    size::Int64
    # RX events
    evts::Vector{Tuple{Int64,Int64}}
    Client() = new(TCPSocket(), 125, 50, 0, 0, 0, Tuple{Int64,Int64}[])
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
    _send_command(c, 0, round(Int64, rx) << 30 | round(Int64, tx))
end

function set_rates!(c::Client; adc::Real, cic::Int64)
    c.adc_rate = adc
    c.cic_rate = cic
    _send_command(c, 1, cic)
end

function set_dac!(c::Client; level::Real)
    lvl = round(Int64, level / 100.0 * 4095)
    _send_command(c, 2, lvl)
end

function set_level!(c::Client; level::Real)
    lvl = round(Int64, level / 100.0 * 32766)
    _send_command(c, 3, lvl)
end

function set_pin!(c::Client; pin::Int64)
    _send_command(c, 4, pin)
end

function clear_pin!(c::Client; pin::Int64)
    _send_command(c, 5, pin)
end

function clear_events!(c::Client)
    c.last_delay = round(Int64, c.adc_rate * c.cic_rate * 2.0)
    c.last_read = 0
    c.size = 0
    empty!(c.evts)
    _send_command(c, 6, 0)
end

function _update_size!(c::Client)
    sz = round(Int64, c.last_delay / (c.cic_rate * 2.0))

    if sz > 0
        push!(c.evts, (c.last_read, sz))
        _send_command(c, 9, c.last_read << 40 | (sz - 1))
    end

    if c.last_read != 0
        c.size += sz
    end

    c.last_delay = 0
    c.last_read = 0
end

function add_event!(c::Client, delay::Real;
    sync::Int64=0, gate::Int64=0, read::Int64=0,
    level::Real=0, tx_phase::Real=0, rx_phase::Real=0)

    dly = round(Int64, delay * c.adc_rate)
    lvl = round(Int64, level / 100.0 * 32766)
    txp = round(Int64, tx_phase / 360.0 * 0x3fffffff)
    rxp = round(Int64, rx_phase / 360.0 * 0x3fffffff)

    _send_command(c, 7, lvl << 44 | gate << 41 | sync << 40 | (dly - 1))
    _send_command(c, 8, rxp << 30 | txp)

    if c.last_read == read
        c.last_delay += dly
    else
        _update_size!(c)
        c.last_delay = dly
        c.last_read = read
    end
end

function read_time!(c::Client)
    _update_size!(c)

    time = Vector{Float32}(undef, c.size)
    keep = 0
    skip = 0

    for (read, size) in c.evts
        if read != 0
            time[keep+1:keep+size] .= Float32.(keep .+ skip .+ (0:size-1))
            keep += size
        else
            skip += size
        end
    end

    return time .* Float32(c.cic_rate * 2.0 / c.adc_rate)
end

function read_data!(c::Client)
    _update_size!(c)

    _send_command(c, 10, c.size)

    data = read(c.socket, c.size * 16)

    reinterpret(ComplexF32, data)
end

end # module PulsedNMR
