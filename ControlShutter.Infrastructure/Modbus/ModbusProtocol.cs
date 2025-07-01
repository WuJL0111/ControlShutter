namespace ControlShutter.Infrastructure.Modbus;

/// <summary>
/// Modbus协议工具类
/// </summary>
public static class ModbusProtocol
{
    private static readonly ushort[] CrcTable = {
        0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
        0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
        0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
        0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
        0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
        0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
        0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
        0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
        0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
        0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
        0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
        0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
        0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
        0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
        0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
        0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
        0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
        0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
        0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
        0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
        0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
        0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
        0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
        0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
        0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
        0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
        0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
        0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
        0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
        0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
        0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
        0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
    };

    /// <summary>
    /// 计算CRC校验码
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="length">长度</param>
    /// <returns>CRC校验码</returns>
    public static ushort CalculateCrc(ReadOnlySpan<byte> data, int length = -1)
    {
        if (data.IsEmpty)
            return 0;

        var actualLength = length == -1 ? data.Length : Math.Min(length, data.Length);
        ushort crc = 0xFFFF;

        for (int i = 0; i < actualLength; i++)
        {
            var tableIndex = (crc ^ data[i]) & 0xFF;
            crc = (ushort)((crc >> 8) ^ CrcTable[tableIndex]);
        }

        return crc;
    }

    /// <summary>
    /// 验证CRC校验码
    /// </summary>
    /// <param name="data">包含CRC的完整数据</param>
    /// <returns>校验是否正确</returns>
    public static bool VerifyCrc(ReadOnlySpan<byte> data)
    {
        if (data.Length < 3)
            return false;

        var calculatedCrc = CalculateCrc(data[..^2]);
        var receivedCrc = (ushort)(data[^2] | (data[^1] << 8));
        
        return calculatedCrc == receivedCrc;
    }

    /// <summary>
    /// 创建写单个数字输出命令 (功能码 0x05)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="ioPort">IO端口</param>
    /// <param name="value">输出值</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateWriteSingleCoilCommand(byte deviceAddress, ushort ioPort, bool value)
    {
        var command = new byte[8];
        command[0] = deviceAddress;         // 设备地址
        command[1] = 0x05;                  // 功能码
        command[2] = (byte)(ioPort >> 8);   // 起始地址高字节
        command[3] = (byte)(ioPort & 0xFF); // 起始地址低字节
        command[4] = (byte)(value ? 0xFF : 0x00); // 输出值高字节
        command[5] = 0x00;                  // 输出值低字节

        var crc = CalculateCrc(command.AsSpan()[..6]);
        command[6] = (byte)(crc & 0xFF);    // CRC低字节
        command[7] = (byte)(crc >> 8);      // CRC高字节

        return command;
    }

    /// <summary>
    /// 创建写多个数字输出命令 (功能码 0x0F)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="coilCount">线圈数量</param>
    /// <param name="values">输出值</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateWriteMultipleCoilsCommand(byte deviceAddress, ushort startAddress, ushort coilCount, ReadOnlySpan<bool> values)
    {
        var byteCount = (coilCount + 7) / 8;
        var command = new byte[9 + byteCount];
        
        command[0] = deviceAddress;                    // 设备地址
        command[1] = 0x0F;                            // 功能码
        command[2] = (byte)(startAddress >> 8);       // 起始地址高字节
        command[3] = (byte)(startAddress & 0xFF);     // 起始地址低字节
        command[4] = (byte)(coilCount >> 8);          // 线圈数量高字节
        command[5] = (byte)(coilCount & 0xFF);        // 线圈数量低字节
        command[6] = (byte)byteCount;                 // 字节数

        // 将布尔值转换为字节
        for (int i = 0; i < byteCount; i++)
        {
            byte byteValue = 0;
            for (int bit = 0; bit < 8 && (i * 8 + bit) < values.Length; bit++)
            {
                if (values[i * 8 + bit])
                {
                    byteValue |= (byte)(1 << bit);
                }
            }
            command[7 + i] = byteValue;
        }

        var crc = CalculateCrc(command.AsSpan()[..^2]);
        command[^2] = (byte)(crc & 0xFF);    // CRC低字节
        command[^1] = (byte)(crc >> 8);      // CRC高字节

        return command;
    }

    /// <summary>
    /// 创建读数字输入命令 (功能码 0x02)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="inputCount">输入数量</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateReadDiscreteInputsCommand(byte deviceAddress, ushort startAddress, ushort inputCount)
    {
        var command = new byte[8];
        command[0] = deviceAddress;                    // 设备地址
        command[1] = 0x02;                            // 功能码
        command[2] = (byte)(startAddress >> 8);       // 起始地址高字节
        command[3] = (byte)(startAddress & 0xFF);     // 起始地址低字节
        command[4] = (byte)(inputCount >> 8);         // 输入数量高字节
        command[5] = (byte)(inputCount & 0xFF);       // 输入数量低字节

        var crc = CalculateCrc(command.AsSpan()[..6]);
        command[6] = (byte)(crc & 0xFF);              // CRC低字节
        command[7] = (byte)(crc >> 8);                // CRC高字节

        return command;
    }

    /// <summary>
    /// 创建读线圈状态命令 (功能码 0x01)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="coilCount">线圈数量</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateReadCoilsCommand(byte deviceAddress, ushort startAddress, ushort coilCount)
    {
        var command = new byte[8];
        command[0] = deviceAddress;                    // 设备地址
        command[1] = 0x01;                            // 功能码
        command[2] = (byte)(startAddress >> 8);       // 起始地址高字节
        command[3] = (byte)(startAddress & 0xFF);     // 起始地址低字节
        command[4] = (byte)(coilCount >> 8);          // 线圈数量高字节
        command[5] = (byte)(coilCount & 0xFF);        // 线圈数量低字节

        var crc = CalculateCrc(command.AsSpan()[..6]);
        command[6] = (byte)(crc & 0xFF);              // CRC低字节
        command[7] = (byte)(crc >> 8);                // CRC高字节

        return command;
    }

    /// <summary>
    /// 创建读保持寄存器命令 (功能码 0x03)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="registerCount">寄存器数量</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateReadHoldingRegistersCommand(byte deviceAddress, ushort startAddress, ushort registerCount)
    {
        var command = new byte[8];
        command[0] = deviceAddress;                    // 设备地址
        command[1] = 0x03;                            // 功能码
        command[2] = (byte)(startAddress >> 8);       // 起始地址高字节
        command[3] = (byte)(startAddress & 0xFF);     // 起始地址低字节
        command[4] = (byte)(registerCount >> 8);      // 寄存器数量高字节
        command[5] = (byte)(registerCount & 0xFF);    // 寄存器数量低字节

        var crc = CalculateCrc(command.AsSpan()[..6]);
        command[6] = (byte)(crc & 0xFF);              // CRC低字节
        command[7] = (byte)(crc >> 8);                // CRC高字节

        return command;
    }

    /// <summary>
    /// 创建读输入寄存器命令 (功能码 0x04)
    /// </summary>
    /// <param name="deviceAddress">设备地址</param>
    /// <param name="startAddress">起始地址</param>
    /// <param name="registerCount">寄存器数量</param>
    /// <returns>Modbus命令数据</returns>
    public static byte[] CreateReadInputRegistersCommand(byte deviceAddress, ushort startAddress, ushort registerCount)
    {
        var command = new byte[8];
        command[0] = deviceAddress;                    // 设备地址
        command[1] = 0x04;                            // 功能码
        command[2] = (byte)(startAddress >> 8);       // 起始地址高字节
        command[3] = (byte)(startAddress & 0xFF);     // 起始地址低字节
        command[4] = (byte)(registerCount >> 8);      // 寄存器数量高字节
        command[5] = (byte)(registerCount & 0xFF);    // 寄存器数量低字节

        var crc = CalculateCrc(command.AsSpan()[..6]);
        command[6] = (byte)(crc & 0xFF);              // CRC低字节
        command[7] = (byte)(crc >> 8);                // CRC高字节

        return command;
    }

    /// <summary>
    /// 解析Modbus响应
    /// </summary>
    /// <param name="response">响应数据</param>
    /// <returns>解析结果</returns>
    public static ModbusResponse ParseResponse(ReadOnlySpan<byte> response)
    {
        if (response.Length < 5)
        {
            return new ModbusResponse { IsValid = false, ErrorMessage = "响应数据长度不足" };
        }

        if (!VerifyCrc(response))
        {
            return new ModbusResponse { IsValid = false, ErrorMessage = "CRC校验失败" };
        }

        var deviceAddress = response[0];
        var functionCode = response[1];

        // 检查是否为错误响应
        if ((functionCode & 0x80) != 0)
        {
            var exceptionCode = response[2];
            return new ModbusResponse 
            { 
                IsValid = false, 
                ErrorMessage = $"Modbus异常，异常码: {exceptionCode}",
                DeviceAddress = deviceAddress,
                FunctionCode = (byte)(functionCode & 0x7F)
            };
        }

        // 根据功能码解析数据
        return functionCode switch
        {
            0x01 or 0x02 => ParseDiscreteResponse(response, deviceAddress, functionCode),
            0x03 or 0x04 => ParseRegisterResponse(response, deviceAddress, functionCode),
            0x05 => ParseWriteSingleCoilResponse(response, deviceAddress, functionCode),
            0x06 => ParseWriteSingleRegisterResponse(response, deviceAddress, functionCode),
            0x0F => ParseWriteMultipleCoilsResponse(response, deviceAddress, functionCode),
            0x10 => ParseWriteMultipleRegistersResponse(response, deviceAddress, functionCode),
            _ => new ModbusResponse { IsValid = false, ErrorMessage = $"不支持的功能码: {functionCode}" }
        };
    }

    private static ModbusResponse ParseDiscreteResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 5)
            return new ModbusResponse { IsValid = false, ErrorMessage = "离散量响应数据长度不足" };

        var byteCount = response[2];
        if (response.Length < 5 + byteCount)
            return new ModbusResponse { IsValid = false, ErrorMessage = "离散量响应数据不完整" };

        var data = response.Slice(3, byteCount).ToArray();
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = data
        };
    }

    private static ModbusResponse ParseRegisterResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 5)
            return new ModbusResponse { IsValid = false, ErrorMessage = "寄存器响应数据长度不足" };

        var byteCount = response[2];
        if (response.Length < 5 + byteCount)
            return new ModbusResponse { IsValid = false, ErrorMessage = "寄存器响应数据不完整" };

        var data = response.Slice(3, byteCount).ToArray();
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = data
        };
    }

    private static ModbusResponse ParseWriteSingleCoilResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 8)
            return new ModbusResponse { IsValid = false, ErrorMessage = "写单个线圈响应数据长度不足" };

        var address = (ushort)((response[2] << 8) | response[3]);
        var value = (ushort)((response[4] << 8) | response[5]);
        
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = BitConverter.GetBytes(value)
        };
    }

    private static ModbusResponse ParseWriteSingleRegisterResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 8)
            return new ModbusResponse { IsValid = false, ErrorMessage = "写单个寄存器响应数据长度不足" };

        var address = (ushort)((response[2] << 8) | response[3]);
        var value = (ushort)((response[4] << 8) | response[5]);
        
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = BitConverter.GetBytes(value)
        };
    }

    private static ModbusResponse ParseWriteMultipleCoilsResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 8)
            return new ModbusResponse { IsValid = false, ErrorMessage = "写多个线圈响应数据长度不足" };

        var startAddress = (ushort)((response[2] << 8) | response[3]);
        var coilCount = (ushort)((response[4] << 8) | response[5]);
        
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = BitConverter.GetBytes(coilCount)
        };
    }

    private static ModbusResponse ParseWriteMultipleRegistersResponse(ReadOnlySpan<byte> response, byte deviceAddress, byte functionCode)
    {
        if (response.Length < 8)
            return new ModbusResponse { IsValid = false, ErrorMessage = "写多个寄存器响应数据长度不足" };

        var startAddress = (ushort)((response[2] << 8) | response[3]);
        var registerCount = (ushort)((response[4] << 8) | response[5]);
        
        return new ModbusResponse
        {
            IsValid = true,
            DeviceAddress = deviceAddress,
            FunctionCode = functionCode,
            Data = BitConverter.GetBytes(registerCount)
        };
    }
}

/// <summary>
/// Modbus响应结果
/// </summary>
public class ModbusResponse
{
    /// <summary>
    /// 响应是否有效
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备地址
    /// </summary>
    public byte DeviceAddress { get; set; }
    
    /// <summary>
    /// 功能码
    /// </summary>
    public byte FunctionCode { get; set; }
    
    /// <summary>
    /// 响应数据
    /// </summary>
    public byte[]? Data { get; set; }
}